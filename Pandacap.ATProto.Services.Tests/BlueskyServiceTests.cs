using Pandacap.ATProto.Services.Interfaces;
using System.Net;
using System.Text;

namespace Pandacap.ATProto.Services.Tests
{
    [TestClass]
    public sealed class BlueskyServiceTests
    {
        private const string PWHL_LIKES_FORWARD_3 = @"{""records"":[{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3m2anjbvvpb2h"",""cid"":""bafyreic2tzeapmb6kyezsmrwy3w2xz4e5t6nkeg35q7uoapm57qpsh7jui"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreidxphxeogojqspfmlbdk33guixwef4zxp3z5rzzubl7cgn76wxdfq"",""uri"":""at://did:plc:fzvwugwhxkw5p3kuumuk2lxg/app.bsky.feed.post/3m2akauoirc2j""},""createdAt"":""2025-10-02T22:54:35.490Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3m2anjaigh22z"",""cid"":""bafyreie2mongldvak54h52wdqotvhvkhd7woajjxmrpx23t72wdhcxbdsu"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreichdrthf5u7bazcl3hkaq2napqabq54tyiwaf6hhl7pzviyq24lhq"",""uri"":""at://did:plc:swuktwgsuoi3k4ihjofrbvlb/app.bsky.feed.post/3m2ajmukaoc2y""},""createdAt"":""2025-10-02T22:54:34.000Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3m2anj6v2vc2z"",""cid"":""bafyreice5csh5enruonrvlifr7eaaknufvsxtr2pa46ahofrgjxzwhu6ue"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreie272vsckbnxbu5avj3gokigczi25nvmx3cbvzv7c6s2psyspeo5m"",""uri"":""at://did:plc:yniv7mcf3mkzyxzcsm4ls2yn/app.bsky.feed.post/3m2akpnmpos24""},""createdAt"":""2025-10-02T22:54:32.246Z""}}],""cursor"":""3m2anj6v2vc2z""}";
        private const string PWHL_LIKES_REVERSE_3 = @"{""records"":[{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lges3k2jnx2y"",""cid"":""bafyreic5gstecccurxl2ihplacuszslblonapdtnz5y5clvihulwydc2bu"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreicznkiqm7ciawdfrgxx2spdgpcdgdnvbaowdk33nzbbvnxsj2wfxe"",""uri"":""at://did:plc:5u3fufzjkr3gi6s6qw5hvv6i/app.bsky.feed.post/3lgeni4rfic24""},""createdAt"":""2025-01-23T02:03:09.227Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lgetk2jydc2g"",""cid"":""bafyreianpaoeys2tzv4i5pzjp2vk57ebhrwmtfiqiatwdqipfoticryyya"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreidswmvalt26zz4fq5a4yvjuap3rlokdtodds5qjrlh4gubrhbup6q"",""uri"":""at://did:plc:qamzzdxoofybsso5iqmfkrpd/app.bsky.feed.post/3lgetjmhji22c""},""createdAt"":""2025-01-23T02:29:10.110Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lgqpyss25y27"",""cid"":""bafyreihmypw6bz6vwqcfru2frl4sbyjlm5yvdlyozqzda4qqmibm6nxvwa"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreid37vls5vnj34vm7qcvcglygwldpltt75yv3pzhoudpljuqcsenpe"",""uri"":""at://did:plc:qamzzdxoofybsso5iqmfkrpd/app.bsky.feed.post/3lgqm6gwglk2q""},""createdAt"":""2025-01-27T19:57:47.059Z""}}],""cursor"":""3lgqpyss25y27""}";

        private const string PWHL_LIKES_FORWARD_20 = @"{""records"":[{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3m2anjbvvpb2h"",""cid"":""bafyreic2tzeapmb6kyezsmrwy3w2xz4e5t6nkeg35q7uoapm57qpsh7jui"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreidxphxeogojqspfmlbdk33guixwef4zxp3z5rzzubl7cgn76wxdfq"",""uri"":""at://did:plc:fzvwugwhxkw5p3kuumuk2lxg/app.bsky.feed.post/3m2akauoirc2j""},""createdAt"":""2025-10-02T22:54:35.490Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3m2anjaigh22z"",""cid"":""bafyreie2mongldvak54h52wdqotvhvkhd7woajjxmrpx23t72wdhcxbdsu"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreichdrthf5u7bazcl3hkaq2napqabq54tyiwaf6hhl7pzviyq24lhq"",""uri"":""at://did:plc:swuktwgsuoi3k4ihjofrbvlb/app.bsky.feed.post/3m2ajmukaoc2y""},""createdAt"":""2025-10-02T22:54:34.000Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3m2anj6v2vc2z"",""cid"":""bafyreice5csh5enruonrvlifr7eaaknufvsxtr2pa46ahofrgjxzwhu6ue"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreie272vsckbnxbu5avj3gokigczi25nvmx3cbvzv7c6s2psyspeo5m"",""uri"":""at://did:plc:yniv7mcf3mkzyxzcsm4ls2yn/app.bsky.feed.post/3m2akpnmpos24""},""createdAt"":""2025-10-02T22:54:32.246Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lr7t6w7sry27"",""cid"":""bafyreierhapxcyksx57koijondohfasxm76cquqfd3d5g6scgkawi5vniq"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreig35pi7ugvfleyzeqzutcktnkmqsgtcwedmytda5sdv2nl5aehvoa"",""uri"":""at://did:plc:ibyyain6xjki2juhkgyiugnt/app.bsky.feed.post/3lr7rppudbk25""},""createdAt"":""2025-06-10T02:16:45.658Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lr7t6msb4527"",""cid"":""bafyreib4s6pg77khyucvot2gs4lltlxf2756ciixby3mdlhxaqxasyk5mm"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreigws22xoj6mmzxjbn42t5ovd6fga2womy4hnpqmjpyxv443mgw7vm"",""uri"":""at://did:plc:7eowu263bzowuoyynxnanuel/app.bsky.feed.post/3lr7qcage3k22""},""createdAt"":""2025-06-10T02:16:35.769Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lr7t6jyydc2k"",""cid"":""bafyreibl4qnfrg5cb6e253ki7fybmxmoimnzyqrrpqmjsg6nyjo2kqt5sy"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreic5uiiobpta2ecgulco3w6ozgucyzsgnmam64iancn7ehoeury56a"",""uri"":""at://did:plc:7eowu263bzowuoyynxnanuel/app.bsky.feed.post/3lr7qembh3c2t""},""createdAt"":""2025-06-10T02:16:32.670Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lr7t5we6bt2b"",""cid"":""bafyreihy2vlppc27iioi2pg6ckvxyxqmb6o6d3jo3c6n6ineuxxz7ui3ba"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreie5sityb524r7s7eqmvtct4hbur4s4ye4zbd5rbehk67wiesvgyzq"",""uri"":""at://did:plc:ibyyain6xjki2juhkgyiugnt/app.bsky.feed.post/3lr7rovpfzk25""},""createdAt"":""2025-06-10T02:16:12.055Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lr7t5kk5i52z"",""cid"":""bafyreiguso6gwbkib36dutdkt4kyl5c6kufxvu4reylqnc5awhpguljmvu"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreihoy2nlsqyxt75ooexqpz5jf5tbaladvxbfonyfkbmr6vhliwocha"",""uri"":""at://did:plc:7eowu263bzowuoyynxnanuel/app.bsky.feed.post/3lr7pqdxqrk2q""},""createdAt"":""2025-06-10T02:15:59.855Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lr7t5gw5gt2c"",""cid"":""bafyreidiq2slyrsufdf4jvygeetp32z47zji2mcb4gyecyrcak5bfqmwbm"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreicbtyjvpwdmih57ez3wtuo46e7za7bckxcvcfumdlls4hcc56v2g4"",""uri"":""at://did:plc:7eowu263bzowuoyynxnanuel/app.bsky.feed.post/3lr7pksadpc22""},""createdAt"":""2025-06-10T02:15:55.876Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lr7t52a7q72s"",""cid"":""bafyreia6bi236ockkyai3zbuesjmjuaf25jltvbnzyhsupg4msbiq3ezue"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreia6yvieljnc6xysssszqkli6dwfus5wqymtqdmek5keet46aleqwu"",""uri"":""at://did:plc:ibyyain6xjki2juhkgyiugnt/app.bsky.feed.post/3lr7p6m4aik26""},""createdAt"":""2025-06-10T02:15:42.552Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lr7t4wimcw2x"",""cid"":""bafyreihhhc5zkid3phvaptffjtvcegee34gkzz4jft3d2mqikefkhubzqe"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreihpwmk5ti7rl3p4cpvfvcazh7nuuz2z5z2hpy7quwuimmzxtf2u4i"",""uri"":""at://did:plc:ibyyain6xjki2juhkgyiugnt/app.bsky.feed.post/3lr7rnd653s25""},""createdAt"":""2025-06-10T02:15:38.646Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lr7t4jj3762q"",""cid"":""bafyreiada6jxa6ljrgxesuywckt7ykoswpkptv3xaxz5dlfenqe36palgq"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreifjrnkzjtsino2v26r37n3jbwp7si4fovbjwi7m4t4yzb46mrt25u"",""uri"":""at://did:plc:7eowu263bzowuoyynxnanuel/app.bsky.feed.post/3lr7ovuqwus2q""},""createdAt"":""2025-06-10T02:15:25.215Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lr7t4h35bc2k"",""cid"":""bafyreibkupvimfgofhuacqzc3zd5vjlw53ve3wx5jq4jylkzeumfyi4cya"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreihs7fka6zu2f5jru523snpkfg7d7iy42mofujguscealwcpk7daqq"",""uri"":""at://did:plc:7eowu263bzowuoyynxnanuel/app.bsky.feed.post/3lr7ouzf3js22""},""createdAt"":""2025-06-10T02:15:22.469Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lr7t42bn4527"",""cid"":""bafyreic6kzsm4f3gxyoksnywkncfezj2qqhavgai2n6vp5nqyzsvyjwlk4"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreid4k5zmr6ra5432jdrwx5ydodqx7f3yjwhw7jwe7om4xka4hqn35a"",""uri"":""at://did:plc:ibyyain6xjki2juhkgyiugnt/app.bsky.feed.post/3lr7rmhomks25""},""createdAt"":""2025-06-10T02:15:09.067Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lr7t3ts3732b"",""cid"":""bafyreifshi6pfs6fsxm7upk2xsb42hahyhxupriyoigado4q2ic6szo3wu"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreiggskywxcxgzhsg5oj2dsvknauttfdean5pmosqjb5irthygcx4rm"",""uri"":""at://did:plc:ibyyain6xjki2juhkgyiugnt/app.bsky.feed.post/3lr7olpashs26""},""createdAt"":""2025-06-10T02:15:02.266Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lr7t3im6wf2z"",""cid"":""bafyreibxrhn4erhgq4ru3yf6moy63li7aqi6bx2jllln7g3euahaxgqbl4"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreidn72jykia7poycgd5mjjhtrdsayjwkxxelvwqdfengj5l5ndft6q"",""uri"":""at://did:plc:7eowu263bzowuoyynxnanuel/app.bsky.feed.post/3lr7nytlwt222""},""createdAt"":""2025-06-10T02:14:50.714Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lqfqdizoqp2k"",""cid"":""bafyreihictiyjq3ke3xga2bbctlh5w7iutq3ti2quxlfadyn3adldf5s7e"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreibdlqp6yibus5jybxk3p7sqe4lyrp375i456wuexedt6exs4ytknm"",""uri"":""at://did:plc:mcrsyjm6y5wvs6by3p7urih6/app.bsky.feed.post/3lqfkp7phks2q""},""createdAt"":""2025-05-30T17:16:25.091Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lpu7sfryo42x"",""cid"":""bafyreidsuomhiqgrvhfzkmk7jo5cxxjjpnonu2aqibeoifhvjd4mkrwxw4"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreif434lspkicnh377e4c2sol4b3lcgh6u3f3dd4ghneevxzucpsbtq"",""uri"":""at://did:plc:7eowu263bzowuoyynxnanuel/app.bsky.feed.post/3lpu7lf66gk2g""},""createdAt"":""2025-05-23T18:05:16.037Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lo542kouq526"",""cid"":""bafyreig27fevq7tduq7ej562jn5qag4w4c5e35wyveu6sxadvtoff6vjum"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreigky7mfyahn554lrk2rcq4a4ps66tijpzr5rrxcw4vqujxrsu5ff4"",""uri"":""at://did:plc:7eowu263bzowuoyynxnanuel/app.bsky.feed.post/3lo4qdlie7k2k""},""createdAt"":""2025-05-01T20:01:48.886Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lnzuy3y3kl23"",""cid"":""bafyreib222mjkzrncigw46bsgfgla6d3grxyidcipd6bowxoce2qzvqkt4"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreibqlotphcvvqcsok2h25olhqhrtjzc7uwb3jz3dwhjed37h77v2fy"",""uri"":""at://did:plc:ibyyain6xjki2juhkgyiugnt/app.bsky.feed.post/3lnztpu7hcs2j""},""createdAt"":""2025-04-30T13:17:10.960Z""}}],""cursor"":""3lnzuy3y3kl23""}";
        private const string PWHL_LIKES_FORWARD_20_NEXT = @"{""records"":[{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lkresc4um72f"",""cid"":""bafyreihgmb6p4t6ltrjt57a3wi262mvn7duhaej3u2h7gac2ljguskgtfq"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreieuyzp3zc64fnbizlfwgqhbae5zvmtdzeeg77bdgns3iupghqs3e4"",""uri"":""at://did:plc:mcrsyjm6y5wvs6by3p7urih6/app.bsky.feed.post/3lkr4iqsja222""},""createdAt"":""2025-03-19T23:50:43.408Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lkgz25yhp22f"",""cid"":""bafyreie67pqwooeeqbfhq3k5siefm7yeqfpgxmkqbb3guudj2taq5kjlxy"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreicuzoga5lpzhfximsb2hsahj3ukdhlgngwpvqh2kl37y3ssbxivqq"",""uri"":""at://did:plc:mcrsyjm6y5wvs6by3p7urih6/app.bsky.feed.post/3lkgxhgsxuk2d""},""createdAt"":""2025-03-15T20:53:45.208Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lkgdsl2st42a"",""cid"":""bafyreihssaorpz2yzkaakjfcbwerkknfdjez5wjkv43mo5dszmj3f6kd2y"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreihcz6avw3zhccskvzv7cupyofmg2dohxmq2inspggkn55iuinxgcy"",""uri"":""at://did:plc:g5hizrds2u45tv3e7xn6a7ka/app.bsky.feed.post/3lk7a3pntds2t""},""createdAt"":""2025-03-15T14:33:41.893Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3ljxti6m2td2f"",""cid"":""bafyreiahxvobo4auybqr5ltttdutodinf657ckrfkxharp7be6mi57ilky"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreie4dob75h7ysgiynssftzgkr4yvrluhwk5h22z2spi4g6fdha7pci"",""uri"":""at://did:plc:mcrsyjm6y5wvs6by3p7urih6/app.bsky.feed.post/3ljxsq33g2c22""},""createdAt"":""2025-03-09T20:04:17.148Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3ljtfunflk32c"",""cid"":""bafyreih7dkdr2yrldb56amxzjcrehskc3s5tnkwpt56akahqgnl4vegnym"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreigjhcqq5bmkrbg37gjyyh37woeszh7fizzdjqameazczqk6dghjve"",""uri"":""at://did:plc:xapdish7eyqntc4mcjoea6f7/app.bsky.feed.post/3ljtesar6ls24""},""createdAt"":""2025-03-08T01:50:03.901Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3ljdga45jhh2m"",""cid"":""bafyreiata65z6nfqwx6dnowpcursrsnhasgdjmbqvkry43472us4uqxh5e"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreidsyohqtry5cabkni246yo3d3tvtajyo2cww3vbvai4nuqo6u6jfm"",""uri"":""at://did:plc:ojomom24wbdnlyhi6lbd5u3m/app.bsky.feed.post/3ljd2affsec2d""},""createdAt"":""2025-03-01T17:13:52.732Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3ljdg7szqos2u"",""cid"":""bafyreidtwosi3rykdpagbwhnewl5zdr6ylvotfih7r5sfjtfobmwqwkesu"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreicxm6fgsjbvi3ome47nsevhjya4xb7i5pnqweleo4od3ohjuhfefi"",""uri"":""at://did:plc:5u3fufzjkr3gi6s6qw5hvv6i/app.bsky.feed.post/3ljd7ox5j6s2j""},""createdAt"":""2025-03-01T17:13:43.171Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lirpsgeblv2k"",""cid"":""bafyreia4svzid3x6yturx5rmtlwsobwdyan7w6t7cvtz7jwlcgrddlx3aq"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreiebi66zh34pqwguivexbgrhl5pel4cuvdquf2tzs53xpbfid5ecba"",""uri"":""at://did:plc:ojomom24wbdnlyhi6lbd5u3m/app.bsky.feed.post/3lgxwsrg4dc2o""},""createdAt"":""2025-02-22T16:17:15.814Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lirpsb6wi22x"",""cid"":""bafyreihe55mwpababtxtcdpghv5pckghpcnvzeiawilawphywjvgwlyfaq"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreiaifj74bp3wjm7q2dekx7swt4i5e3h3czcxdkh6lt52hkudzc6r6m"",""uri"":""at://did:plc:g5hizrds2u45tv3e7xn6a7ka/app.bsky.feed.post/3lirk3cnxgc2y""},""createdAt"":""2025-02-22T16:17:10.309Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3licl3giefp2n"",""cid"":""bafyreiebaxbijzbx7oenatzuhjtl5ld4a5t7ye6wwcchm5rzuzik5qngii"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreihrmthfo4brjkdiiqpubuefck72igkoavjl4jkxvifuqtnqopyfmu"",""uri"":""at://did:plc:mcrsyjm6y5wvs6by3p7urih6/app.bsky.feed.post/3liciyjmtv222""},""createdAt"":""2025-02-16T15:42:53.119Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3licl3apeu72h"",""cid"":""bafyreicm24r32df6m7ftwf4csp7pv3xxgp2ssftkecvbwbootkyjkxeygq"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreidd77mae6tmwmkenjziz55vppty3qfkd7hkhcppyof4wvsfebbbsy"",""uri"":""at://did:plc:5u3fufzjkr3gi6s6qw5hvv6i/app.bsky.feed.post/3licfchjljc2l""},""createdAt"":""2025-02-16T15:42:46.967Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lhvwj5j74a2f"",""cid"":""bafyreigp3jwxetk5cicqn3rk7osbatrmw2rpndt3jloefdc37d3j4ardrm"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreiendvhdxqtflbyzn5feifqieivuyfoejxie66qvsvv3ln57oa7mtu"",""uri"":""at://did:plc:qamzzdxoofybsso5iqmfkrpd/app.bsky.feed.post/3lhvwhp5zfs2q""},""createdAt"":""2025-02-11T15:02:47.948Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lh4plw7gdg2i"",""cid"":""bafyreiglhcdzub3pu2faj26lfmg6ve5fmqn7f2s2gz2p5hzqvj44rjk6o4"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreiaed3nswoqapcw5taix7kqxdu3n556cek622zwqzqi5mnv2ebhw4y"",""uri"":""at://did:plc:g5hizrds2u45tv3e7xn6a7ka/app.bsky.feed.post/3lh4opa24f22y""},""createdAt"":""2025-02-01T14:22:31.314Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lgqpyss25y27"",""cid"":""bafyreihmypw6bz6vwqcfru2frl4sbyjlm5yvdlyozqzda4qqmibm6nxvwa"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreid37vls5vnj34vm7qcvcglygwldpltt75yv3pzhoudpljuqcsenpe"",""uri"":""at://did:plc:qamzzdxoofybsso5iqmfkrpd/app.bsky.feed.post/3lgqm6gwglk2q""},""createdAt"":""2025-01-27T19:57:47.059Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lgetk2jydc2g"",""cid"":""bafyreianpaoeys2tzv4i5pzjp2vk57ebhrwmtfiqiatwdqipfoticryyya"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreidswmvalt26zz4fq5a4yvjuap3rlokdtodds5qjrlh4gubrhbup6q"",""uri"":""at://did:plc:qamzzdxoofybsso5iqmfkrpd/app.bsky.feed.post/3lgetjmhji22c""},""createdAt"":""2025-01-23T02:29:10.110Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lges3k2jnx2y"",""cid"":""bafyreic5gstecccurxl2ihplacuszslblonapdtnz5y5clvihulwydc2bu"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreicznkiqm7ciawdfrgxx2spdgpcdgdnvbaowdk33nzbbvnxsj2wfxe"",""uri"":""at://did:plc:5u3fufzjkr3gi6s6qw5hvv6i/app.bsky.feed.post/3lgeni4rfic24""},""createdAt"":""2025-01-23T02:03:09.227Z""}}],""cursor"":""3lges3k2jnx2y""}";

        private const string PWHL_LIKES_REVERSE_20 = @"{""records"":[{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lges3k2jnx2y"",""cid"":""bafyreic5gstecccurxl2ihplacuszslblonapdtnz5y5clvihulwydc2bu"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreicznkiqm7ciawdfrgxx2spdgpcdgdnvbaowdk33nzbbvnxsj2wfxe"",""uri"":""at://did:plc:5u3fufzjkr3gi6s6qw5hvv6i/app.bsky.feed.post/3lgeni4rfic24""},""createdAt"":""2025-01-23T02:03:09.227Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lgetk2jydc2g"",""cid"":""bafyreianpaoeys2tzv4i5pzjp2vk57ebhrwmtfiqiatwdqipfoticryyya"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreidswmvalt26zz4fq5a4yvjuap3rlokdtodds5qjrlh4gubrhbup6q"",""uri"":""at://did:plc:qamzzdxoofybsso5iqmfkrpd/app.bsky.feed.post/3lgetjmhji22c""},""createdAt"":""2025-01-23T02:29:10.110Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lgqpyss25y27"",""cid"":""bafyreihmypw6bz6vwqcfru2frl4sbyjlm5yvdlyozqzda4qqmibm6nxvwa"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreid37vls5vnj34vm7qcvcglygwldpltt75yv3pzhoudpljuqcsenpe"",""uri"":""at://did:plc:qamzzdxoofybsso5iqmfkrpd/app.bsky.feed.post/3lgqm6gwglk2q""},""createdAt"":""2025-01-27T19:57:47.059Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lh4plw7gdg2i"",""cid"":""bafyreiglhcdzub3pu2faj26lfmg6ve5fmqn7f2s2gz2p5hzqvj44rjk6o4"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreiaed3nswoqapcw5taix7kqxdu3n556cek622zwqzqi5mnv2ebhw4y"",""uri"":""at://did:plc:g5hizrds2u45tv3e7xn6a7ka/app.bsky.feed.post/3lh4opa24f22y""},""createdAt"":""2025-02-01T14:22:31.314Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lhvwj5j74a2f"",""cid"":""bafyreigp3jwxetk5cicqn3rk7osbatrmw2rpndt3jloefdc37d3j4ardrm"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreiendvhdxqtflbyzn5feifqieivuyfoejxie66qvsvv3ln57oa7mtu"",""uri"":""at://did:plc:qamzzdxoofybsso5iqmfkrpd/app.bsky.feed.post/3lhvwhp5zfs2q""},""createdAt"":""2025-02-11T15:02:47.948Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3licl3apeu72h"",""cid"":""bafyreicm24r32df6m7ftwf4csp7pv3xxgp2ssftkecvbwbootkyjkxeygq"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreidd77mae6tmwmkenjziz55vppty3qfkd7hkhcppyof4wvsfebbbsy"",""uri"":""at://did:plc:5u3fufzjkr3gi6s6qw5hvv6i/app.bsky.feed.post/3licfchjljc2l""},""createdAt"":""2025-02-16T15:42:46.967Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3licl3giefp2n"",""cid"":""bafyreiebaxbijzbx7oenatzuhjtl5ld4a5t7ye6wwcchm5rzuzik5qngii"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreihrmthfo4brjkdiiqpubuefck72igkoavjl4jkxvifuqtnqopyfmu"",""uri"":""at://did:plc:mcrsyjm6y5wvs6by3p7urih6/app.bsky.feed.post/3liciyjmtv222""},""createdAt"":""2025-02-16T15:42:53.119Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lirpsb6wi22x"",""cid"":""bafyreihe55mwpababtxtcdpghv5pckghpcnvzeiawilawphywjvgwlyfaq"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreiaifj74bp3wjm7q2dekx7swt4i5e3h3czcxdkh6lt52hkudzc6r6m"",""uri"":""at://did:plc:g5hizrds2u45tv3e7xn6a7ka/app.bsky.feed.post/3lirk3cnxgc2y""},""createdAt"":""2025-02-22T16:17:10.309Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lirpsgeblv2k"",""cid"":""bafyreia4svzid3x6yturx5rmtlwsobwdyan7w6t7cvtz7jwlcgrddlx3aq"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreiebi66zh34pqwguivexbgrhl5pel4cuvdquf2tzs53xpbfid5ecba"",""uri"":""at://did:plc:ojomom24wbdnlyhi6lbd5u3m/app.bsky.feed.post/3lgxwsrg4dc2o""},""createdAt"":""2025-02-22T16:17:15.814Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3ljdg7szqos2u"",""cid"":""bafyreidtwosi3rykdpagbwhnewl5zdr6ylvotfih7r5sfjtfobmwqwkesu"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreicxm6fgsjbvi3ome47nsevhjya4xb7i5pnqweleo4od3ohjuhfefi"",""uri"":""at://did:plc:5u3fufzjkr3gi6s6qw5hvv6i/app.bsky.feed.post/3ljd7ox5j6s2j""},""createdAt"":""2025-03-01T17:13:43.171Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3ljdga45jhh2m"",""cid"":""bafyreiata65z6nfqwx6dnowpcursrsnhasgdjmbqvkry43472us4uqxh5e"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreidsyohqtry5cabkni246yo3d3tvtajyo2cww3vbvai4nuqo6u6jfm"",""uri"":""at://did:plc:ojomom24wbdnlyhi6lbd5u3m/app.bsky.feed.post/3ljd2affsec2d""},""createdAt"":""2025-03-01T17:13:52.732Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3ljtfunflk32c"",""cid"":""bafyreih7dkdr2yrldb56amxzjcrehskc3s5tnkwpt56akahqgnl4vegnym"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreigjhcqq5bmkrbg37gjyyh37woeszh7fizzdjqameazczqk6dghjve"",""uri"":""at://did:plc:xapdish7eyqntc4mcjoea6f7/app.bsky.feed.post/3ljtesar6ls24""},""createdAt"":""2025-03-08T01:50:03.901Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3ljxti6m2td2f"",""cid"":""bafyreiahxvobo4auybqr5ltttdutodinf657ckrfkxharp7be6mi57ilky"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreie4dob75h7ysgiynssftzgkr4yvrluhwk5h22z2spi4g6fdha7pci"",""uri"":""at://did:plc:mcrsyjm6y5wvs6by3p7urih6/app.bsky.feed.post/3ljxsq33g2c22""},""createdAt"":""2025-03-09T20:04:17.148Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lkgdsl2st42a"",""cid"":""bafyreihssaorpz2yzkaakjfcbwerkknfdjez5wjkv43mo5dszmj3f6kd2y"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreihcz6avw3zhccskvzv7cupyofmg2dohxmq2inspggkn55iuinxgcy"",""uri"":""at://did:plc:g5hizrds2u45tv3e7xn6a7ka/app.bsky.feed.post/3lk7a3pntds2t""},""createdAt"":""2025-03-15T14:33:41.893Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lkgz25yhp22f"",""cid"":""bafyreie67pqwooeeqbfhq3k5siefm7yeqfpgxmkqbb3guudj2taq5kjlxy"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreicuzoga5lpzhfximsb2hsahj3ukdhlgngwpvqh2kl37y3ssbxivqq"",""uri"":""at://did:plc:mcrsyjm6y5wvs6by3p7urih6/app.bsky.feed.post/3lkgxhgsxuk2d""},""createdAt"":""2025-03-15T20:53:45.208Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lkresc4um72f"",""cid"":""bafyreihgmb6p4t6ltrjt57a3wi262mvn7duhaej3u2h7gac2ljguskgtfq"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreieuyzp3zc64fnbizlfwgqhbae5zvmtdzeeg77bdgns3iupghqs3e4"",""uri"":""at://did:plc:mcrsyjm6y5wvs6by3p7urih6/app.bsky.feed.post/3lkr4iqsja222""},""createdAt"":""2025-03-19T23:50:43.408Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lnzuy3y3kl23"",""cid"":""bafyreib222mjkzrncigw46bsgfgla6d3grxyidcipd6bowxoce2qzvqkt4"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreibqlotphcvvqcsok2h25olhqhrtjzc7uwb3jz3dwhjed37h77v2fy"",""uri"":""at://did:plc:ibyyain6xjki2juhkgyiugnt/app.bsky.feed.post/3lnztpu7hcs2j""},""createdAt"":""2025-04-30T13:17:10.960Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lo542kouq526"",""cid"":""bafyreig27fevq7tduq7ej562jn5qag4w4c5e35wyveu6sxadvtoff6vjum"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreigky7mfyahn554lrk2rcq4a4ps66tijpzr5rrxcw4vqujxrsu5ff4"",""uri"":""at://did:plc:7eowu263bzowuoyynxnanuel/app.bsky.feed.post/3lo4qdlie7k2k""},""createdAt"":""2025-05-01T20:01:48.886Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lpu7sfryo42x"",""cid"":""bafyreidsuomhiqgrvhfzkmk7jo5cxxjjpnonu2aqibeoifhvjd4mkrwxw4"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreif434lspkicnh377e4c2sol4b3lcgh6u3f3dd4ghneevxzucpsbtq"",""uri"":""at://did:plc:7eowu263bzowuoyynxnanuel/app.bsky.feed.post/3lpu7lf66gk2g""},""createdAt"":""2025-05-23T18:05:16.037Z""}},{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lqfqdizoqp2k"",""cid"":""bafyreihictiyjq3ke3xga2bbctlh5w7iutq3ti2quxlfadyn3adldf5s7e"",""value"":{""$type"":""app.bsky.feed.like"",""subject"":{""cid"":""bafyreibdlqp6yibus5jybxk3p7sqe4lyrp375i456wuexedt6exs4ytknm"",""uri"":""at://did:plc:mcrsyjm6y5wvs6by3p7urih6/app.bsky.feed.post/3lqfkp7phks2q""},""createdAt"":""2025-05-30T17:16:25.091Z""}}],""cursor"":""3lqfqdizoqp2k""}";

        [TestMethod]
        public async Task BlueskyService_GetNewestLikesAsync_ReturnsLessThanTwenty()
        {
            var cancellationToken = CancellationToken.None;

            var did = $"{Guid.NewGuid()}";
            var pds = "www.example.com";
            var uriPath = $"https://{pds}/xrpc/com.atproto.repo.listRecords";

            var regularDictionary = new Dictionary<Uri, string>
            {
                [new($"{uriPath}?repo={did}&collection={"app.bsky.feed.like"}&limit=20")] = PWHL_LIKES_FORWARD_3,
                [new($"{uriPath}?repo={did}&collection={"app.bsky.feed.like"}&limit=20&reverse=true")] = PWHL_LIKES_REVERSE_3,
                [new($"{uriPath}?repo={did}&collection={"app.bsky.feed.like"}&limit=20&cursor=3m2anj6v2vc2z")] = @"{""records"":[]}",
                [new($"{uriPath}?repo={did}&collection={"app.bsky.feed.like"}&limit=20&cursor=3lgqpyss25y27&reverse=true")] = @"{""records"":[]}",
            };

            var invertedDictionary = new Dictionary<Uri, string>
            {
                [new($"{uriPath}?repo={did}&collection={"app.bsky.feed.like"}&limit=20")] = PWHL_LIKES_REVERSE_3,
                [new($"{uriPath}?repo={did}&collection={"app.bsky.feed.like"}&limit=20&reverse=true")] = PWHL_LIKES_FORWARD_3,
                [new($"{uriPath}?repo={did}&collection={"app.bsky.feed.like"}&limit=20&cursor=3lgqpyss25y27")] = @"{""records"":[]}",
                [new($"{uriPath}?repo={did}&collection={"app.bsky.feed.like"}&limit=20&cursor=3m2anj6v2vc2z&reverse=true")] = @"{""records"":[]}",
            };

            foreach (var dict in new[] { regularDictionary, invertedDictionary })
            {
                var mockRequestHandler = new MockRequestHandler(dict);

                IBlueskyService service = new BlueskyService(mockRequestHandler);

                var actual = await service.GetNewestLikesAsync(pds, did).ToListAsync(cancellationToken);

                Assert.HasCount(3, actual);

                Assert.AreEqual(
                    actual: actual[0],
                    expected: new(
                        new(
                            cID: "bafyreic2tzeapmb6kyezsmrwy3w2xz4e5t6nkeg35q7uoapm57qpsh7jui",
                            uri: new("at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3m2anjbvvpb2h")),
                        new(
                            createdAt: DateTimeOffset.Parse("2025-10-02T22:54:35.490Z"),
                            subject: new(
                                cID: "bafyreidxphxeogojqspfmlbdk33guixwef4zxp3z5rzzubl7cgn76wxdfq",
                                uri: new("at://did:plc:fzvwugwhxkw5p3kuumuk2lxg/app.bsky.feed.post/3m2akauoirc2j")))));

                Assert.AreEqual(
                    actual: actual[2],
                    expected: new(
                        new(
                            cID: "bafyreice5csh5enruonrvlifr7eaaknufvsxtr2pa46ahofrgjxzwhu6ue",
                            uri: new("at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3m2anj6v2vc2z")),
                        new(
                            createdAt: DateTimeOffset.Parse("2025-10-02T22:54:32.246Z"),
                            subject: new(
                                cID: "bafyreie272vsckbnxbu5avj3gokigczi25nvmx3cbvzv7c6s2psyspeo5m",
                                uri: new("at://did:plc:yniv7mcf3mkzyxzcsm4ls2yn/app.bsky.feed.post/3m2akpnmpos24")))));
            }
        }

        [TestMethod]
        public async Task BlueskyService_GetNewestLikesAsync_ReturnsMoreThanTwenty()
        {
            var cancellationToken = CancellationToken.None;

            var did = $"{Guid.NewGuid()}";
            var pds = "www.example.com";
            var uriPath = $"https://{pds}/xrpc/com.atproto.repo.listRecords";

            var regularDictionary = new Dictionary<Uri, string>
            {
                [new($"{uriPath}?repo={did}&collection={"app.bsky.feed.like"}&limit=20")] = PWHL_LIKES_FORWARD_20,
                [new($"{uriPath}?repo={did}&collection={"app.bsky.feed.like"}&limit=20&reverse=true")] = PWHL_LIKES_REVERSE_20,
                [new($"{uriPath}?repo={did}&collection={"app.bsky.feed.like"}&limit=20&cursor=3lnzuy3y3kl23")] = PWHL_LIKES_FORWARD_20_NEXT,
                [new($"{uriPath}?repo={did}&collection={"app.bsky.feed.like"}&limit=20&cursor=3lges3k2jnx2y")] = @"{""records"":[]}",
                [new($"{uriPath}?repo={did}&collection={"app.bsky.feed.like"}&limit=20&cursor=3lqfqdizoqp2k&reverse=true")] = @"{""records"":[]}",
            };

            var invertedDictionary = new Dictionary<Uri, string>
            {
                [new($"{uriPath}?repo={did}&collection={"app.bsky.feed.like"}&limit=20")] = PWHL_LIKES_REVERSE_20,
                [new($"{uriPath}?repo={did}&collection={"app.bsky.feed.like"}&limit=20&reverse=true")] = PWHL_LIKES_FORWARD_20,
                [new($"{uriPath}?repo={did}&collection={"app.bsky.feed.like"}&limit=20&cursor=3lqfqdizoqp2k")] = @"{""records"":[]}",
                [new($"{uriPath}?repo={did}&collection={"app.bsky.feed.like"}&limit=20&cursor=3lnzuy3y3kl23&reverse=true")] = PWHL_LIKES_FORWARD_20_NEXT,
                [new($"{uriPath}?repo={did}&collection={"app.bsky.feed.like"}&limit=20&cursor=3lges3k2jnx2y&reverse=true")] = @"{""records"":[]}",
            };

            foreach (var dict in new[] { regularDictionary, invertedDictionary })
            {
                var mockRequestHandler = new MockRequestHandler(dict);

                IBlueskyService service = new BlueskyService(mockRequestHandler);

                var actual = await service.GetNewestLikesAsync(pds, did).ToListAsync(cancellationToken);

                Assert.HasCount(36, actual);

                Assert.AreEqual(
                    actual: actual[0],
                    expected: new(
                        new(
                            cID: "bafyreic2tzeapmb6kyezsmrwy3w2xz4e5t6nkeg35q7uoapm57qpsh7jui",
                            uri: new("at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3m2anjbvvpb2h")),
                        new(
                            createdAt: DateTimeOffset.Parse("2025-10-02T22:54:35.490Z"),
                            subject: new(
                                cID: "bafyreidxphxeogojqspfmlbdk33guixwef4zxp3z5rzzubl7cgn76wxdfq",
                                uri: new("at://did:plc:fzvwugwhxkw5p3kuumuk2lxg/app.bsky.feed.post/3m2akauoirc2j")))));

                Assert.AreEqual(
                    actual: actual[22],
                    expected: new(
                        new(
                            cID: "bafyreihssaorpz2yzkaakjfcbwerkknfdjez5wjkv43mo5dszmj3f6kd2y",
                            uri: new("at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3lkgdsl2st42a")),
                        new(
                            createdAt: DateTimeOffset.Parse("2025-03-15T14:33:41.893Z"),
                            subject: new(
                                cID: "bafyreihcz6avw3zhccskvzv7cupyofmg2dohxmq2inspggkn55iuinxgcy",
                                uri: new("at://did:plc:g5hizrds2u45tv3e7xn6a7ka/app.bsky.feed.post/3lk7a3pntds2t")))));
            }
        }

        [TestMethod]
        public async Task BlueskyService_GetNewestLikesAsync_OnlyTakesFive()
        {
            var cancellationToken = CancellationToken.None;

            var did = $"{Guid.NewGuid()}";
            var pds = "www.example.com";
            var uriPath = $"https://{pds}/xrpc/com.atproto.repo.listRecords";

            var regularDictionary = new Dictionary<Uri, string>
            {
                [new($"{uriPath}?repo={did}&collection={"app.bsky.feed.like"}&limit=20")] = PWHL_LIKES_FORWARD_20,
                [new($"{uriPath}?repo={did}&collection={"app.bsky.feed.like"}&limit=20&reverse=true")] = PWHL_LIKES_REVERSE_20
            };

            var invertedDictionary = new Dictionary<Uri, string>
            {
                [new($"{uriPath}?repo={did}&collection={"app.bsky.feed.like"}&limit=20")] = PWHL_LIKES_REVERSE_20,
                [new($"{uriPath}?repo={did}&collection={"app.bsky.feed.like"}&limit=20&reverse=true")] = PWHL_LIKES_FORWARD_20
            };

            foreach (var dict in new[] { regularDictionary, invertedDictionary })
            {
                var mockRequestHandler = new MockRequestHandler(dict);

                IBlueskyService service = new BlueskyService(mockRequestHandler);

                var actual = await service.GetNewestLikesAsync(pds, did).Take(5).ToListAsync(cancellationToken);

                Assert.HasCount(5, actual);

                Assert.AreEqual(
                    actual: actual[0],
                    expected: new(
                        new(
                            cID: "bafyreic2tzeapmb6kyezsmrwy3w2xz4e5t6nkeg35q7uoapm57qpsh7jui",
                            uri: new("at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.like/3m2anjbvvpb2h")),
                        new(
                            createdAt: DateTimeOffset.Parse("2025-10-02T22:54:35.490Z"),
                            subject: new(
                                cID: "bafyreidxphxeogojqspfmlbdk33guixwef4zxp3z5rzzubl7cgn76wxdfq",
                                uri: new("at://did:plc:fzvwugwhxkw5p3kuumuk2lxg/app.bsky.feed.post/3m2akauoirc2j")))));
            }
        }

        [TestMethod]
        public async Task BlueskyService_GetProfileAsync_AcquiresSingleProfile()
        {
            var cancellationToken = CancellationToken.None;

            var did = $"{Guid.NewGuid()}";
            var pds = "www.example.com";
            var uriPath = $"https://{pds}/xrpc/com.atproto.repo.listRecords";

            var dict = new Dictionary<Uri, string>
            {
                [new($"{uriPath}?repo={did}&collection={"app.bsky.actor.profile"}&limit=20")] = @"{""records"":[{""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.actor.profile/self"",""cid"":""bafyreibexevsu2imwbwvepcy4t5cwasljdqxt2xc7hwtumy7fkim542oua"",""value"":{""$type"":""app.bsky.actor.profile"",""avatar"":{""ref"":{""$link"":""bafkreif632npb2cghp52u4jhotzvgwal52i3dqtbgskditifhaxxudzvte""},""size"":537487,""$type"":""blob"",""mimeType"":""image/jpeg""},""banner"":{""ref"":{""$link"":""bafkreicxy2fokh7vxfihwazlhby57dalgro36bjuuu4r2ux6e7f6m4incm""},""size"":552823,""$type"":""blob"",""mimeType"":""image/jpeg""},""createdAt"":""2024-12-03T23:25:07.142Z"",""pinnedPost"":{""cid"":""bafyreid2guz2h3atyjx5emua4cq4frfpn7eooofs2re3ird2sghiusn7yi"",""uri"":""at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.feed.post/3m3ayjqsht22i""},""description"":""The League Legends Call Home"",""displayName"":""PWHL""}}],""cursor"":""self""}"
            };

            var mockRequestHandler = new MockRequestHandler(dict);

            IBlueskyService service = new BlueskyService(mockRequestHandler);

            var actual = await service.GetProfileAsync(pds, did, cancellationToken);

            Assert.AreEqual(
                actual: actual,
                expected: new(
                    new(
                        cID: "bafyreibexevsu2imwbwvepcy4t5cwasljdqxt2xc7hwtumy7fkim542oua",
                        uri: new("at://did:plc:6y5lt7n7msot7rm3mgom3knp/app.bsky.actor.profile/self")),
                    new(
                        avatarCID: "bafkreif632npb2cghp52u4jhotzvgwal52i3dqtbgskditifhaxxudzvte",
                        displayName: "PWHL",
                        description: "The League Legends Call Home")));
        }

        [TestMethod]
        public async Task BlueskyService_GetProfileAsync_AcquiresNoProfile()
        {
            var cancellationToken = CancellationToken.None;

            var did = $"{Guid.NewGuid()}";
            var pds = "www.example.com";
            var uriPath = $"https://{pds}/xrpc/com.atproto.repo.listRecords";

            var dict = new Dictionary<Uri, string>
            {
                [new($"{uriPath}?repo={did}&collection={"app.bsky.actor.profile"}&limit=20")] = @"{""records"":[],""cursor"":""self""}"
            };

            var mockRequestHandler = new MockRequestHandler(dict);

            IBlueskyService service = new BlueskyService(mockRequestHandler);

            var actual = await service.GetProfileAsync(pds, did, cancellationToken);

            Assert.IsNull(actual);
        }

        private class MockRequestHandler(IReadOnlyDictionary<Uri, string> ResponseMap) : IATProtoRequestHandler
        {
            public async Task<HttpResponseMessage> GetJsonAsync(Uri uri, CancellationToken cancellationToken)
            {
                if (ResponseMap.TryGetValue(uri, out var json))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                throw new NotImplementedException($"No setup for URI {uri}");
            }
        }
    }
}
