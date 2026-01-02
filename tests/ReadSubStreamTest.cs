using System.Runtime.CompilerServices;
using OpenCompote.SGA.CustomStreams;

namespace OpenCompote.SGA.Tests;

public class ReadSubStreamTest
{

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        var baseStream = new MemoryStream([0,1,2,3,4,5,6,7,8,9]);
        var subStream = new ReadSubStream(baseStream, 0, 5);
        
        Assert.NotNull(subStream);
        Assert.Equal(5, subStream.Length);
        Assert.Equal(0, subStream.Position);
        Assert.True(subStream.CanRead);
    }

    [Fact]
    public void Read_ReadBufferCorrectly()
    {
        var baseStream = new MemoryStream([0,1,2,3,4,5,6,7,8,9]);
        var subStream = new ReadSubStream(baseStream, 5, 5);

        Assert.Equal(5, subStream.Length);
        Assert.Equal(0, subStream.Position);

        var buffer = new byte[5];
        subStream.ReadExactly(buffer);
        
        Assert.Equal(5, subStream.Position);
        Assert.Equal(10, baseStream.Position);
        Assert.Equal([5,6,7,8,9],buffer);
    }

    [Fact]
    public void Read_ReadBufferAfterEnd()
    {
        var baseStream = new MemoryStream([0,1,2,3,4,5,6,7,8,9]);
        var subStream = new ReadSubStream(baseStream, 2, 5);

        var buffer = new byte[8];
        var returned = subStream.Read(buffer);

        Assert.Equal(5, subStream.Position);
        Assert.Equal(5, returned);
        Assert.Equal([2,3,4,5,6,0,0,0], buffer);
    }

    [Fact]
    public void Read_MultiSubStreamReads()
    {
        var baseStream = new MemoryStream([0,1,2,3,4,5,6,7,8,9]);
        var subStream1 = new ReadSubStream(baseStream, 2, 5);
        var subStream2 = new ReadSubStream(baseStream, 3, 5);

        Assert.Equal(3, subStream2.ReadByte());

        var buffer1 = new byte [5];
        var buffer2 = new byte [4];

        var read1 = subStream1.Read(buffer1);
        var read2 = subStream2.Read(buffer2);

        Assert.Equal(5, read1);
        Assert.Equal(4, read2);
        Assert.Equal([2,3,4,5,6],buffer1);
        Assert.Equal([4,5,6,7],buffer2);
    }
}
