using System.Buffers;
using System.Text;
using System.Text.Json;
using RazorComponentHelpers;

namespace RazorComponentHelpers.Test;

public class JsonConverterTest
{
    private readonly StringBooleanConverter _boolConverter = new();
    private readonly StringNullIntConverter _nullableIntConverter = new();
    private readonly JsonSerializerOptions _options = new();

    [Theory]
    [InlineData("\"true\"", true)]
    [InlineData("\"FALSE\"", false)]
    public void StringBooleanConverter_Reads_StringTokens(string json, bool expected)
    {
        var reader = CreateReader(json);
        var result = _boolConverter.Read(ref reader, typeof(bool), _options);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void StringBooleanConverter_Reads_BooleanTokens(string json, bool expected)
    {
        var reader = CreateReader(json);
        var result = _boolConverter.Read(ref reader, typeof(bool), _options);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("\"maybe\"")]
    [InlineData("123")]
    public void StringBooleanConverter_Read_InvalidTokens_Throws(string json)
    {
        Assert.Throws<JsonException>(() =>
        {
            var reader = CreateReader(json);
            _boolConverter.Read(ref reader, typeof(bool), _options);
        });
    }

    [Fact]
    public void StringBooleanConverter_Writes_BooleanTokens()
    {
        var json = WriteJson(writer => _boolConverter.Write(writer, true, _options));
        Assert.Equal("true", json);
    }

    [Theory]
    [InlineData("null", null)]
    [InlineData("123", 123)]
    [InlineData("\"456\"", 456)]
    [InlineData("\"   \"", null)]
    public void StringNullIntConverter_Reads_VariousTokens(string json, int? expected)
    {
        var reader = CreateReader(json);
        var result = _nullableIntConverter.Read(ref reader, typeof(int?), _options);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void StringNullIntConverter_Read_InvalidToken_Throws()
    {
        Assert.Throws<JsonException>(() =>
        {
            var reader = CreateReader("\"invalid\"");
            _nullableIntConverter.Read(ref reader, typeof(int?), _options);
        });
    }

    [Theory]
    [InlineData(42, "42")]
    [InlineData(null, "null")]
    public void StringNullIntConverter_Writes_ExpectedJson(int? value, string expectedJson)
    {
        var json = WriteJson(writer => _nullableIntConverter.Write(writer, value, _options));
        Assert.Equal(expectedJson, json);
    }

    private static Utf8JsonReader CreateReader(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        var reader = new Utf8JsonReader(bytes);
        reader.Read();
        return reader;
    }

    private static string WriteJson(Action<Utf8JsonWriter> writeAction)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        writeAction(writer);
        writer.Flush();
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}