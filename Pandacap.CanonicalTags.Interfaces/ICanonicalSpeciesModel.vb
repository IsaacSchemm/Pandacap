Imports Pandacap.Database

Public Interface ICanonicalSpeciesModel
    ReadOnly Property Species As CanonicalSpecies
    ReadOnly Property PartOf As IEnumerable(Of CanonicalSpecies)
End Interface
