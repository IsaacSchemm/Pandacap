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

    const loadMedia = async (src: string) => {
        // Called when the user selects a media link from the menu (if handlers
        // are set up); may be called on page load for the first media link.

        try {
            // Store old player's source and seek time
            const oldPlayer = player();
            const oldSrc = oldPlayer?.src;
            const oldTime = oldPlayer?.currentTimeMs();

            player(null);

            // Initialize the player
            const PlayerClass = PPS.casting()
                ? CastjsPlayer
                : PPSPlayer;

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

    const directLink = document.getElementById("directLink");
    if (directLink instanceof HTMLAnchorElement)
        loadMedia(directLink.href);
}
