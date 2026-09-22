using FluentAssertions;
using Ogasela.Shared;

namespace Ogasela.UnitTests;

public class ResultTests
{
    [Fact]
    public void Success_ReturnsIsSuccessTrue_AndNoError()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_ReturnsIsSuccessFalse_AndCarriesError()
    {
        var error = new Error("Test.Failure", "Something went wrong.");

        var result = Result.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void GenericSuccess_ExposesValue()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void GenericFailure_ThrowsWhenAccessingValue()
    {
        var result = Result.Failure<int>("Test.Failure", "Something went wrong.");

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>();
    }
}
