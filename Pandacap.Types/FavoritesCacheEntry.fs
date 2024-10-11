namespace Pandacap.Types

type FavoritesCacheEntry<'I, 'P> =
| Item of 'I
| PageBoundary of 'P
