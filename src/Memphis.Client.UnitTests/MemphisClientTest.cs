using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using Memphis.Client.Constants;
using Memphis.Client.Exception;
using Memphis.Client.Models.Response;
using Memphis.Client.Validators;
using Moq;
using NATS.Client;
using NATS.Client.JetStream;
using Xunit;
using Xunit.Abstractions;

namespace Memphis.Client.UnitTests
{
    public class MemphisClientTest
    {
        private readonly Options _brokerOptions;
        private readonly string _connectionId;
        private readonly Mock<IConnection> _connectionMock;
        private readonly Mock<IJetStream> _jetStreamMock;

        private readonly ConcurrentDictionary<ValidatorType, ISchemaValidator> _schemaValidators;
        
        private readonly Mock<ISchemaValidator> _graphqlValidatorMock;
        private readonly Mock<ISchemaValidator> _jsonValidatorMock;
        private readonly Mock<ISchemaValidator> _protobufValidatorMock;

        private readonly ITestOutputHelper _testOutputHelper;

        private readonly MemphisClient _sut;

        #pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public MemphisClientTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;

            this._brokerOptions = ConnectionFactory.GetDefaultOptions();
            this._brokerOptions.User = "root";
            this._connectionId = Guid.NewGuid().ToString();
            this._connectionMock = new Mock<IConnection>();
            this._jetStreamMock = new Mock<IJetStream>();

            _sut = new MemphisClient(_brokerOptions, _connectionMock.Object, _jetStreamMock.Object, _connectionId);

            this._graphqlValidatorMock = new Mock<ISchemaValidator>();
            this._jsonValidatorMock = new Mock<ISchemaValidator>();
            this._protobufValidatorMock = new Mock<ISchemaValidator>();

            this._schemaValidators = new ConcurrentDictionary<ValidatorType, ISchemaValidator>();
            this._schemaValidators.TryAdd(ValidatorType.GRAPHQL, _graphqlValidatorMock.Object);
            this._schemaValidators.TryAdd(ValidatorType.JSON, _jsonValidatorMock.Object);
            this._schemaValidators.TryAdd(ValidatorType.PROTOBUF, _protobufValidatorMock.Object);

            TestingUtil.setFieldToValue(_sut, "_schemaValidators", _schemaValidators);
        }

        #region MemphisClient.ValidateMessageAsync

        [Fact]
        public async Task ShouldDoSuccess_WhenValidateMessageAsync_WhereNoSchemaFound()
        {
            // given:
            var schemaUpdateDictionaryMock = this.getEmptySchemaUpdateDataMock();
            TestingUtil.setFieldToValue(_sut, "_schemaUpdateDictionary", schemaUpdateDictionaryMock);

            var givenMsgBytes = Encoding.UTF8.GetBytes("{ sayHello }");
            var givenInternalStationName = "test-station-name-which-not-exist-schema";
            var givenProducerName = "test-producer-name";
            
            // when:
            await _sut.ValidateMessageAsync(givenMsgBytes, givenInternalStationName, givenProducerName);

            // then:
        }

        [Fact]
        public async Task ShouldDoCallSchemaValidator_WhenValidateMessageAsync_WhereGraphqlMessageOkWithSchema()
        {
            // given:
            var schemaUpdateDictionaryMock = this.getSchemaUpdateDataMock();
            TestingUtil.setFieldToValue(_sut, "_schemaUpdateDictionary", schemaUpdateDictionaryMock);

            var givenMsgBytes = Encoding.UTF8.GetBytes("{ sayHello }");
            var givenInternalStationName = "test-station-name-graphql-01";
            var givenProducerName = "test-producer-name";

            _graphqlValidatorMock.Setup(
                    mock => mock.ValidateAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // when:
            await _sut.ValidateMessageAsync(givenMsgBytes, givenInternalStationName, givenProducerName);

            // then:
            _graphqlValidatorMock.Verify(
                mock => mock.ValidateAsync(It.IsAny<byte[]>(), It.IsAny<string>()), Times.Once);
        }
        
        [Fact]
        public async Task ShouldDoThrowExceptionAndSendNotification_WhenValidateMessageAsync_WhereGraphqlMessageNotOkWithSchema()
        {
            // given:
            var schemaUpdateDictionaryMock = this.getSchemaUpdateDataMock();
            TestingUtil.setFieldToValue(_sut, "_schemaUpdateDictionary", schemaUpdateDictionaryMock);

            var givenMsgBytes = Encoding.UTF8.GetBytes("{ sayHello888 }");
            var givenInternalStationName = "test-station-name-graphql-01";
            var givenProducerName = "test-producer-name";

            _graphqlValidatorMock.Setup(
                    mock => mock.ValidateAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .ThrowsAsync(new MemphisSchemaValidationException("Schema validation occurred"));

            // when:
            await Assert.ThrowsAsync<MemphisSchemaValidationException>(
                () => _sut.ValidateMessageAsync(givenMsgBytes, givenInternalStationName, givenProducerName));
            
            // then:
            _graphqlValidatorMock.Verify(
                mock => mock.ValidateAsync(It.IsAny<byte[]>(), It.IsAny<string>()), Times.Once);
            _connectionMock.Verify(
                mock => mock.Publish(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Once);
        }


        private ConcurrentDictionary<string, SchemaUpdateInit> getSchemaUpdateDataMock()
        {
            var schemaUpdateDictionaryMock = new ConcurrentDictionary<string, SchemaUpdateInit>();
            var internalStationNameForGraphql = "test-station-name-graphql-01";
            var graphqlSchemaStr = @"
               type Query {
                   sayHello : String
                  }
             ";
            schemaUpdateDictionaryMock.TryAdd(internalStationNameForGraphql, new SchemaUpdateInit()
            {
                SchemaName = "test-schema-01",
                SchemaType = MemphisSchemaTypes.GRAPH_QL,
                ActiveVersion = new ProducerSchemaUpdateVersion()
                {
                    Content = graphqlSchemaStr,
                    Descriptor = "test-description",
                    VersionNumber = "1",
                    MessageStructName = "test-struct-name"
                }
            });

            return schemaUpdateDictionaryMock;
        }

        private ConcurrentDictionary<string, SchemaUpdateInit> getEmptySchemaUpdateDataMock()
        {
            return new ConcurrentDictionary<string, SchemaUpdateInit>();
        }

        #endregion
    }
}