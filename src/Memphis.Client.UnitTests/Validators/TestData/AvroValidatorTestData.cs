using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text;
using SolTechnology.Avro;
using SolTechnology.Avro.Infrastructure.Attributes;

namespace Memphis.Client.UnitTests;


public class ValidUserModel
{
    [DataMember(Name = "name")]
    public string Name { get; set; } = null!;

    [DataMember(Name = "age")]
    public long Age { get; set; }
}

public class InvalidUserModel
{

    [DataMember(Name = "email")]
    public string Email { get; set; } = null!;
}


public class AvroValidatorTestData
{
    public static List<object[]> ValidSchema => new()
    {
        new object[] { validUser },
    };

    public static List<object[]> InvalidSchema => new()
    {
        new object[] { invalidSch1 },
    };

    public static List<object[]> Message => new()
    {
        new object[] { validUserData },
    };

    public static List<object[]> ValidSchemaDetail => new()
    {
        new object[] { "valid-simple-msg", validUser, validUserData },
    };

    public static List<object[]> InvalidSchemaDetail => new()
    {
        new object[] { "invalid-simple-msg", validUser, invalidMsg },
    };


    private static readonly string validUser = AvroConvert.GenerateSchema(typeof(ValidUserModel));


    private const string invalidSch1 = "This is invalid avro schema";


    private readonly static byte[] validUserData = AvroConvert.Serialize(new ValidUserModel
    {
        Name = "John Doe",
        Age = 30
    });


    private readonly static byte[] invalidMsg = AvroConvert.Serialize(new InvalidUserModel
    {
        Email = "test@web.com"
    });

}


