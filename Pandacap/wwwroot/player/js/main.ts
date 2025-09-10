/// <reference path="knockout.d.ts" />

declare var Castjs: any;

namespace PPS {
    export const cjs = typeof Castjs === "function"
        ? new Castjs()
        : null;

    if (cjs) {
        cjs.on("connect", () => casting(true));
        cjs.on("disconnect", () => casting(false));
    }

    export const casting = ko.observable(cjs?.connected);

    const player = ko.observable<PPSPlayer | CastjsPlayer>();

    casting.subscribe(() => {
        const pl = player();
        if (!pl)
            return;

        loadMedia(pl.src);
    });

    ko.applyBindings({
        player,
        paused: ko.pureComputed(() => {
            const pl = player();
            return pl ? !pl.playing() : false;
        }),
        play: () => {
            const pl = player();
            if (pl) {
                pl.play();
            }
        }
    }, document.getElementsByTagName("main")[0]);

    export const selectPlayerTypeAsync = async (src: string) => {
        if (PPS.casting())
            return CastjsPlayer;
        else
            return PPSPlayer;
    }

    const loadMedia = async (src: string) => {
        // Called when the user selects a media link from the menu (if handlers
        // are set up); may be called on page load for the first media link.

        try {
            // Clean up previous player (if any)
            const oldPlayer = player();

            // Store old player's source and seek time
            const oldSrc = oldPlayer?.src;
            const oldTime = oldPlayer?.currentTimeMs();

            if (oldPlayer) {
                player(null);
                oldPlayer.destroy();
            }

            // Initialize the player
            const PlayerClass = await selectPlayerTypeAsync(src);

            const pl = new PlayerClass(
                document.getElementById("video-parent")!,
                src);

            // Bind the player controls
            player(pl);

            // If it's the same media as before, try to seek to the same point
            // (for better Google Cast experience)
            if (pl.src === oldSrc) {
                // Request autoplay in this situation
                pl.play();

                // Wait for media to start playing
                while (!pl.playing()) {
                    await new Promise<void>(r => pl.playing.subscribe(() => r()));
                }

                // Seek to the same timestamp that the player was at previously
                pl.currentTimeMs(oldTime);
            }
        } catch (e) {
            console.error(e);
        }
    }

    // These media links were placed on the page, and currently are just
    // normal links to the media URLs.

    // Loop through these links, and then attach click handlers so the links
    // open the video on the page itself, instead.

    const mediaLinks = document.querySelectorAll("a.media");

    for (let i = 0; i < mediaLinks.length; i++) {
        const mediaLink = mediaLinks[i];

        mediaLink.addEventListener("click", e => {
            e.preventDefault();

            // Close the menu
            document.getElementById("menu").removeAttribute("open");

            // Load the media
            loadMedia(mediaLinks[i].getAttribute("href"));
        });
    }

    if (mediaLinks.length > 0) {
        loadMedia(mediaLinks[0].getAttribute("href"));
    }
}
