Public Interface ICanonicalCharacterAppearanceDisplayInfo
    ReadOnly Property Character As ICanonicalCharacterDisplayInfo
    ReadOnly Property AlternateSpecies As IEnumerable(Of ICanonicalSpeciesDisplayInfo)
End Interface
