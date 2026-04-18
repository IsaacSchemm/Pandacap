Imports System.IO

Public Interface ISvgRenderer
    Sub RenderToPng(input As Stream,
                    output As Stream)

    Function RenderToPng(input As Stream) As Byte()
End Interface
