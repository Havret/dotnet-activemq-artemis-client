using System.IO;
using System.Text;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.TestUtils
{
    public class XUnitOutputAdapter : TextWriter
    {
        private readonly ITestOutputHelper _output;
        public XUnitOutputAdapter(ITestOutputHelper output) => _output = output;
        public override void WriteLine(string value) => _output.WriteLine(value);
        public override Encoding Encoding { get; }
    }
}

