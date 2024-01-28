using System.Collections.Concurrent;
using System.Text;
using GraphQL.Types;
using Memphis.Client.Constants;
using Memphis.Client.Exception;
using Memphis.Client.Validators;
using Xunit.Abstractions;

namespace Memphis.Client.UnitTests.Validators
{
    public class GraphqlValidatorTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly ISchemaValidator _sut;


        public GraphqlValidatorTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _sut = new GraphqlValidator();
        }


        #region GraphqlValidatorTest.ParseAndStore

        [Fact]
        public void ShouldReturnTrue_WhenParseAndStore_WhereValidSchemaPassed()
        {
            // given:
            var schemaStr = @"
               type Query {
                   sayHello : String
                  }
             ";
            var schemaUpdate = ValidatorTestHelper.GetSchemaUpdateInit(
                "test-schema-001",
                schemaStr,
                MemphisSchemaTypes.GRAPH_QL
            );
            // when:

            var actual = _sut.AddOrUpdateSchema(schemaUpdate);

            // then:

            Assert.True(actual);
        }

        [Fact]
        public void ShouldReturnFalse_WhenParseAndStore_WhereInValidSchemaPassed()
        {
            // given:
            var schemaStr = @"
               typ----e Query {
                   sayHelwlo : String
                  }
             ";

            var schemaUpdate = ValidatorTestHelper.GetSchemaUpdateInit(
                "test-schema-001",
                schemaStr,
                MemphisSchemaTypes.GRAPH_QL
            );
            // when:

            var actual = _sut.AddOrUpdateSchema(schemaUpdate);

            // then:

            Assert.False(actual);
        }

        #endregion


        #region GraphqlValidatorTest.ValidateAsync

        [Fact]
        public async Task ShouldDoSuccess_WhenValidateAsync_WhereValidDataPassed()
        {
            // given:
            var givenSchemaData = this.getSchemaCacheMock();
            TestingUtil.setFieldToValue(_sut, "_schemaCache", givenSchemaData);

            var givenMsg = "{ sayHello }";
            var msgToValidate = Encoding.UTF8.GetBytes(givenMsg);

            // when && then
            await _sut.ValidateAsync(msgToValidate, "valid-schema-01");
        }

        [Fact]
        public async Task ShouldDoThrow_WhenValidateAsync_WhereInValidDataPassed()
        {
            // given:
            var givenSchemaData = this.getSchemaCacheMock();
            TestingUtil.setFieldToValue(_sut, "_schemaCache", givenSchemaData);

            var givenMsg = "{ sayHello123 }";
            var msgToValidate = Encoding.UTF8.GetBytes(givenMsg);

            // when && then
            await Assert.ThrowsAsync<MemphisSchemaValidationException>(
                () => _sut.ValidateAsync(msgToValidate, "valid-schema-01"));
        }

        [Fact]
        public async Task ShouldDoThrow_WhenValidateAsync_WhereSchemaNotFoundInCache()
        {
            // given:
            var givenSchemaData = this.getSchemaCacheMock();
            TestingUtil.setFieldToValue(_sut, "_schemaCache", givenSchemaData);

            var givenMsg = "{ sayHel1lo }";
            var msgToValidate = Encoding.UTF8.GetBytes(givenMsg);

            // when && then
            await Assert.ThrowsAsync<MemphisSchemaValidationException>(
                () => _sut.ValidateAsync(msgToValidate, "not-existed-schema"));
        }


        private ConcurrentDictionary<string, ISchema> getSchemaCacheMock()
        {
            var schemaDictionary = new ConcurrentDictionary<string, ISchema>();
            var schemaStr = @"
               type Query {
                   sayHello : String
                  }
             ";
            var schema = Schema.For(schemaStr);
            schemaDictionary.TryAdd("valid-schema-01", schema);

            return schemaDictionary;
        }
        
        #endregion
    }
}