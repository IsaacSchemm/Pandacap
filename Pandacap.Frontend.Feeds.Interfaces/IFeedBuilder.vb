Imports Pandacap.Database

Public Interface IFeedBuilder
    Function ToRssFeed(feedUrl As String,
                       posts As IEnumerable(Of Post)) As String

    Function ToAtomFeed(feedUrl As String,
                        posts As IEnumerable(Of Post)) As String
End Interface
