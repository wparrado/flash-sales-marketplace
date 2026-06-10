using AwesomeAssertions;
using FlashSales.Domain.Shared;

namespace FlashSales.UnitTests.Domain;

public class ResultTests
{
    private static readonly Error SomeError = new("test.error", "Something went wrong");

    [Fact]
    public void Success_ExposesValue()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Failure_ExposesError_AndAccessingValueThrows()
    {
        var result = Result<int>.Failure(SomeError);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(SomeError);
        var act = () => result.Value;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Map_TransformsSuccessValue()
    {
        Result<int>.Success(21).Map(x => x * 2).Value.Should().Be(42);
    }

    [Fact]
    public void Map_PropagatesFailureUntouched()
    {
        Result<int>.Failure(SomeError).Map(x => x * 2).Error.Should().Be(SomeError);
    }

    [Fact]
    public void Bind_ChainsSuccessfulOperations()
    {
        var result = Result<int>.Success(10)
            .Bind(x => x > 5 ? Result<string>.Success($"big:{x}") : Result<string>.Failure(SomeError));

        result.Value.Should().Be("big:10");
    }

    [Fact]
    public void Bind_ShortCircuitsOnFirstFailure()
    {
        var secondStepRan = false;

        var result = Result<int>.Failure(SomeError)
            .Bind(x =>
            {
                secondStepRan = true;
                return Result<int>.Success(x);
            });

        result.IsSuccess.Should().BeFalse();
        secondStepRan.Should().BeFalse();
    }

    [Fact]
    public void Match_SelectsBranchByState()
    {
        Result<int>.Success(1).Match(v => "ok", e => "fail").Should().Be("ok");
        Result<int>.Failure(SomeError).Match(v => "ok", e => e.Code).Should().Be("test.error");
    }

    [Fact]
    public async Task BindAsync_ChainsAsyncOperations()
    {
        var result = await Result<int>.Success(5)
            .BindAsync(x => Task.FromResult(Result<int>.Success(x + 1)));

        result.Value.Should().Be(6);
    }
}
