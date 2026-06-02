namespace Pandacap.CanonicalTags.Tree.Models

open System

type CanonicalTagTreeDisplayNode = {
    Id: Nullable<Guid>
    Name: string
    Type: CanonicalTagType
    Children: CanonicalTagTreeDisplayNode list
}
