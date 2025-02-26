using AutoFixture;
using PubSub.Core.Services.Serializer;
using System.Text;

namespace PubSub.Test.Unit.Core
{
    public class UTF8MessageSerializerTest
    {
        private Fixture _fixture;
        private UTF8MessageSerializer _sut;

        public UTF8MessageSerializerTest() {
            _fixture = new Fixture();
            _sut = new UTF8MessageSerializer();
        }

        [Fact]
        public void Can_Serialize()
        {

            var message = _fixture.Create<string>();

            var result = _sut.Serialize(message);

            var expected = Encoding.UTF8.GetBytes(message);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Can_Deserialize()
        {

            var bytes = _fixture.Create<byte[]>();
            var expected = Encoding.UTF8.GetString(bytes);

            var result = _sut.Deserialize(bytes);

            Assert.Equal(expected, result);
        }
    }
}
