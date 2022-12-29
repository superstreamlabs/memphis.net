using System.Text;

namespace Memphis.Client.UnitTests.Validators.TestData;

internal class JsonValidatorTestData
{

    public static IEnumerable<object[]> ValidSchema => new List<object[]>
    {
        new object[] { simpleSch },
        new object[] { greetingSch },
        new object[] { userInfoSch },
    };

    public static IEnumerable<object[]> InvalidSchema => new List<object[]>
    {
        new object[] { invalidSch1 },
        new object[] { invalidSch2 },
        new object[] { invalidSch3 },
    };

    public static IEnumerable<object[]> Message => new List<object[]>
    {
        new object[] { Encoding.UTF8.GetBytes(simpleMsg) },
        new object[] { Encoding.UTF8.GetBytes(greetingMsg) },
        new object[] { Encoding.UTF8.GetBytes(userInfoMsg) },
    };

    public static IEnumerable<object[]> ValidSchemaDetail => new List<object[]>
    {
        new object[] { "valid-simple-msg", simpleSch, Encoding.UTF8.GetBytes(simpleMsg) },
        new object[] { "valid-greeting-msg", greetingSch, Encoding.UTF8.GetBytes(greetingMsg) },
        new object[] { "valid-userinfo-msg", userInfoSch, Encoding.UTF8.GetBytes(userInfoMsg) },
    };

    public static IEnumerable<object[]> InvalidSchemaDetail => new List<object[]>
    {
        new object[] { "invalid-simple-msg", simpleSch, Encoding.UTF8.GetBytes(invalidMsg) },
        new object[] { "invalid-greeting-msg", greetingSch, Encoding.UTF8.GetBytes(invalidMsg) },
        new object[] { "invalid-userinfo-msg", userInfoSch, Encoding.UTF8.GetBytes(invalidMsg) },
    };



    private const string simpleSch = """
        {
            "$schema": "http://json-schema.org/draft-04/schema#",
            "title": "SimpleMessage",
            "type": "object",
            "additionalProperties": false,
            "required": [
                "Message"
            ],
            "properties": {
                "Message": {
                "type": "string",
                "minLength": 1
                }
            }
        }
    """;

    private const string greetingSch = """
        {
            "$schema": "http://json-schema.org/draft-04/schema#",
            "title": "Greeting",
            "type": "object",
            "additionalProperties": false,
            "required": [
                "From",
                "Message"
            ],
            "properties": {
                "From": {
                "type": "string",
                "minLength": 1
                },
                "Message": {
                "type": "string",
                "minLength": 1
                }
            }
        }
        """;

    private const string userInfoSch = """
    {
        "$schema": "http://json-schema.org/draft-04/schema#",
        "title": "UserInfo",
        "type": "object",
        "additionalProperties": false,
        "required": [
            "FirstName",
            "LastName"
        ],
        "properties": {
            "FirstName": {
            "type": "string",
            "minLength": 1
            },
            "MiddleName": {
            "type": [
                "null",
                "string"
            ]
            },
            "LastName": {
            "type": "string",
            "minLength": 1
            },
            "Gender": {
            "$ref": "#/definitions/Gender"
            }
        },
        "definitions": {
            "Gender": {
            "type": "integer",
            "description": "",
            "x-enumNames": [
                "Male",
                "Female"
            ],
            "enum": [
                0,
                1
            ]
            }
        }
    }
    """;

    private const string invalidSch1 = """
        {
          "type": "object",
          "properties": {
            "name": {
              "type": "string"
            },
            "age": {
              "type": "integer"
            },
            "email": {
              "type": "string",
              "format": "email"
            }
          },
          "required": ["name", "age"],
          "additionalProperties": false
          "invalid_key": "this is invalid because it is not a recognized keyword in JSON schema"
        }
        """;

    private const string invalidSch2 = """
        {
            "type": "object"
            "properties": {
                "name": {
                "type": "string"
                }
            },
            "required": ["name"],
            "additionalProperties": false
        }
    """;

    private const string invalidSch3 = """
        {
          "type": "object",
          "properties": {
            "name": { "type": "string" }
          },
          "invalidKey": { "type": string }
        }  
        """;

    private const string simpleMsg = """
        {
            "Message":"Hello"
        }
        """;

    private const string greetingMsg = """
        {
          "From": "Memphis",
          "Message": "Welcome" 
        }
        """;

    private const string userInfoMsg = """
        {
          "FirstName": "John",
          "LastName": "Doe",
          "Gender": 1  
        }
        """;

    private const string invalidMsg = """
        {
          "MessageType":"Invalid"
        }
        """;


}


