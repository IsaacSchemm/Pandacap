namespace Pandacap.Weasyl.Models.WeasylUpload

type Folder = {
    FolderId: int
    Name: string
} with
    override this.ToString() =
        $"{this.Name} ({this.FolderId})"

type SubmissionType =
| Sketch = 1010
| Traditional = 1020
| Digital = 1030
| Animation = 1040
| Photography = 1050
| Design_Interface = 1060
| Modeling_Sculpture = 1070
| Crafts_Jewelry = 1075
| Sewing_Knitting = 1078
| Desktop_Wallpaper = 1080
| Other = 1999

type Rating =
| General = 10
| Mature = 30
| Explicit = 40
