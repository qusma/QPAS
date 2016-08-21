using MathNet.Numerics.LinearAlgebra.Double;
using NUnit.Framework;
using QPAS;

namespace QPASTest
{
    [TestFixture]
    public class MdsTest
    {
        [Test]
        public void ItWorks()
        {
            DenseMatrix dist = DenseMatrix.Create(3, 3, (_, __) => 0);
            dist[1, 0] = 4.94760097137838;
            dist[2, 0] = 3.48822665076897;

            dist[0, 1] = 4.94760097137838;
            dist[2, 1] = 5.07828347170446;

            dist[0, 2] = 3.48822665076897;
            dist[1, 2] = 5.07828347170446;

            var result = MultiDimensionalScaling.Scale(dist);
            //Assert.IsTrue(false);
            //TODO finish writing
        }
    }
}
