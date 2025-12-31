using OpenCompote.SGA.CustomStreams;

namespace OpenCompote.SGA.Tests;

public class WrapperStreamTest
{   
    [Fact]
    public void Constructor_InitializesStream()
    {
        using var baseStream = new MemoryStream();
        var wrapper = new WrapperStream(baseStream, null);
        
        Assert.NotNull(wrapper);
        Assert.Equal(baseStream.CanRead, wrapper.CanRead);
        Assert.Equal(baseStream.CanSeek, wrapper.CanSeek);
        Assert.Equal(baseStream.CanWrite, wrapper.CanWrite);
    }

    [Fact]
    public void Operations_ReadWriteSeekTest()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        using var baseStream = new MemoryStream();
        baseStream.Write(data);
        baseStream.Position = 0;
        using var wrapper = new WrapperStream(baseStream, null);

        // Reading test
        var buffer = new byte[3];
        int bytesRead = wrapper.Read(buffer, 0, 3);
        
        Assert.Equal(3, bytesRead);
        Assert.Equal(new byte[] { 1, 2, 3 }, buffer);
        Assert.Equal(3, baseStream.Position);
        Assert.Equal(3, wrapper.Position);

        // Seeking test
        wrapper.Position = 5;
        
        Assert.Equal(5, wrapper.Position);
        Assert.Equal(5, baseStream.Position);

        // Writing test
        wrapper.Write([6,7,8]);

        Assert.Equal(baseStream.Position, wrapper.Position);
        Assert.Equal(8, wrapper.Length);
        Assert.Equal(8, baseStream.Length);
        
        wrapper.Position = 0;
        buffer = new byte[8];

        wrapper.ReadExactly(buffer);
        Assert.Equal(new byte[] {1,2,3,4,5,6,7,8}, buffer);
    }

    [Fact]
    public void Dispose_ThrowsOnSubsequentOperations()
    {
        using var baseStream = new MemoryStream();
        var wrapper = new WrapperStream(baseStream, null);
        
        wrapper.Dispose();
        
        Assert.False(wrapper.CanRead);
        Assert.Throws<ObjectDisposedException>(() => wrapper.Position);
        Assert.Throws<ObjectDisposedException>(() => wrapper.Length);
        Assert.Throws<ObjectDisposedException>(wrapper.Flush);
        Assert.Throws<ObjectDisposedException>(() => wrapper.Read(new byte[1], 0, 1));
        Assert.Throws<ObjectDisposedException>(() => wrapper.Seek(10,0));
        Assert.Throws<ObjectDisposedException>(() => wrapper.SetLength(10));
        Assert.Throws<ObjectDisposedException>(() => wrapper.Write(new byte[1], 0, 1));

        // Test - Subsequent Dispose call should not throw an exception!
        wrapper.Dispose();

        // Test - Base stream should not be disposed when wrapper is closed and closeStream = false
        Assert.Equal(0, baseStream.Length);
    }

    [Fact]
    public void Dispose_WithCloseStream_DisposesBaseStream()
    {
        var baseStream = new MemoryStream();
        using var wrapper = new WrapperStream(baseStream, null, closeStream: true);
        wrapper.Dispose();
        
        Assert.Throws<ObjectDisposedException>(() => baseStream.ReadByte());
    }

    [Fact]
    public void Dispose_CallsOnDisposeAction()
    {
        using var baseStream = new MemoryStream();
        bool called = false;
        var wrapper = new WrapperStream(baseStream, () => called = true);
        
        wrapper.Dispose();

        Assert.True(called);

        // If wrapper is disposed again OnDispose should not run again.
        called = false;
        wrapper.Dispose();

        Assert.False(called);
    }
}
