namespace Pandacap.LowLevel

type FavoritesCacheEntry<'I, 'P> =
| Item of 'I
| PageBoundary of 'P
