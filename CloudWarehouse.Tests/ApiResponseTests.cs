using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Tests;

public class ApiResponseTests
{
    [Fact]
    public void ApiResponse_Ok_SetsSuccessAndData()
    {
        var response = ApiResponse.Ok("done");
        Assert.True(response.Success);
        Assert.Equal("done", response.Data);
        Assert.Null(response.Message);
    }

    [Fact]
    public void ApiResponse_Fail_SetsMessage()
    {
        var response = ApiResponse.Fail("error");
        Assert.False(response.Success);
        Assert.Equal("error", response.Message);
    }

    [Fact]
    public void ApiResponseGeneric_Ok_SetsData()
    {
        var response = ApiResponse<int>.Ok(42);
        Assert.True(response.Success);
        Assert.Equal(42, response.Data);
    }

    [Fact]
    public void ApiResponseGeneric_Fail_HasNoData()
    {
        var response = ApiResponse<string>.Fail("missing");
        Assert.False(response.Success);
        Assert.Null(response.Data);
    }
}
