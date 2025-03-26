using PubSub.Core.Services.Serializer;
using PubSub.Test.Base.Tests.Toolkit.Patterns;
using System.Text;

namespace PubSub.Test.Unit.Core
{
    public class JsonPayloadSerializerTest: WithAutoFixture
    {
        private JsonPayloadSerializer _sut;

        public JsonPayloadSerializerTest() {
            _sut = new JsonPayloadSerializer();
        }

        [Fact]
        public void Can_Serialize()
        {

            var message = A<string>();

            //var result = _sut.Serialize(message);

            var expected = Encoding.UTF8.GetBytes(message);
            //Assert.Equal(expected, result);
        }

        [Fact]
        public void Can_Deserialize()
        {

            var bytes = A<byte[]>();
            var expected = Encoding.UTF8.GetString(bytes);

            var result = _sut.Deserialize(bytes);

            //Assert.Equal(expected, result);
        }
    }
}
