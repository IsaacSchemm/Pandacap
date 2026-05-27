Imports Pandacap.Database

Public Interface ICanonicalCharacterModel
    ReadOnly Property Character As CanonicalCharacter
    ReadOnly Property Settings As IEnumerable(Of CanonicalSetting)
    ReadOnly Property Relationships As IEnumerable(Of ICanonicalCharacterRelationshipModel)
    ReadOnly Property AlternateVersions As IEnumerable(Of CanonicalCharacter)
End Interface
