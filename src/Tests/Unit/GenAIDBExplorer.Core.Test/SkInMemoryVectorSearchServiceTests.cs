using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GenAIDBExplorer.Core.SemanticVectors.Search;
using GenAIDBExplorer.Core.SemanticVectors.Records;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.Extensions.VectorData;
using GenAIDBExplorer.Core.Repository.Performance;

namespace GenAIDBExplorer.Core.Test
{
    [TestClass]
    public class SkInMemoryVectorSearchServiceTests
    {
        private Mock<IVectorStoreAdapter> _adapterMock = null!;
        private Mock<IPerformanceMonitor> _perfMock = null!;
        private SkInMemoryVectorSearchService _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _adapterMock = new Mock<IVectorStoreAdapter>(MockBehavior.Strict);
            _perfMock = new Mock<IPerformanceMonitor>(MockBehavior.Strict);
            _sut = new SkInMemoryVectorSearchService(_adapterMock.Object, _perfMock.Object);
        }

        [TestMethod]
        public async Task SearchAsync_TopK_ZeroOrNegative_ThrowsArgumentOutOfRange()
        {
            await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => _sut.SearchAsync(new float[1], 0, new GenAIDBExplorer.Core.SemanticVectors.Infrastructure.VectorInfrastructure("col", "prov")));
            await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => _sut.SearchAsync(new float[1], -1, new GenAIDBExplorer.Core.SemanticVectors.Infrastructure.VectorInfrastructure("col", "prov")));
        }

        [TestMethod]
        public async Task SearchAsync_EmptyVector_ReturnsEmpty()
        {
            var results = await _sut.SearchAsync(ReadOnlyMemory<float>.Empty, 5, new GenAIDBExplorer.Core.SemanticVectors.Infrastructure.VectorInfrastructure("col","prov"));
            results.Should().BeEmpty();
        }

        private static async IAsyncEnumerable<SearchResult<EntityVectorRecord>> AsyncYield(IEnumerable<SearchResult<EntityVectorRecord>> items)
        {
            foreach (var it in items) { yield return it; await Task.Yield(); }
        }

        [TestMethod]
        public async Task SearchAsync_ResultScoreNull_ReturnsZeroScore()
        {
            var record = new EntityVectorRecord();
            var result = new SearchResult<EntityVectorRecord>(record, score: null);

            var collectionMock = new Mock<IVectorSearchable<EntityVectorRecord>>();
            collectionMock.Setup(m => m.SearchAsync(It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<int>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
                .Returns((ReadOnlyMemory<float> v, int k, object? o, CancellationToken ct) => AsyncYield(new[] { result }));

            _adapterMock.Setup(a => a.GetCollection<string, EntityVectorRecord>(It.IsAny<string>())).Returns(collectionMock.Object);
            _perfMock.Setup(p => p.StartOperation(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>())).Returns(Mock.Of<IDisposable>());

            var results = (await _sut.SearchAsync(new float[] { 1f }.AsMemory(), 3, new GenAIDBExplorer.Core.SemanticVectors.Infrastructure.VectorInfrastructure("mycol","prov"))).ToList();

            results.Should().HaveCount(1);
            results[0].Score.Should().Be(0d);
        }

        [TestMethod]
        public async Task SearchAsync_VectorStoreExceptionDoesNotExist_RetriesAndReturns()
        {
            var callCount = 0;
            var good = new SearchResult<EntityVectorRecord>(new EntityVectorRecord(), 0.42);
            var collectionMock = new Mock<IVectorSearchable<EntityVectorRecord>>();
            collectionMock.Setup(m => m.SearchAsync(It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<int>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
                .Returns((ReadOnlyMemory<float> v, int k, object? o, CancellationToken ct) =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        throw new VectorStoreException("Collection does not exist");
                    }
                    return AsyncYield(new[] { good });
                });

            _adapterMock.Setup(a => a.GetCollection<string, EntityVectorRecord>(It.IsAny<string>())).Returns(collectionMock.Object);
            _perfMock.Setup(p => p.StartOperation(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>())).Returns(Mock.Of<IDisposable>());

            var results = (await _sut.SearchAsync(new float[] { 1f }.AsMemory(), 5, new GenAIDBExplorer.Core.SemanticVectors.Infrastructure.VectorInfrastructure("c", "p"))).ToList();

            results.Should().HaveCount(1);
            callCount.Should().Be(2);
        }

        [TestMethod]
        public async Task SearchAsync_UsesPerformanceMonitor()
        {
            var perfOp = Mock.Of<IDisposable>();
            var perfMock = new Mock<IPerformanceMonitor>();
            perfMock.Setup(p => p.StartOperation("Vector.Search", It.IsAny<IDictionary<string, object>>())).Returns(perfOp);

            var collectionMock = new Mock<IVectorSearchable<EntityVectorRecord>>();
            collectionMock.Setup(m => m.SearchAsync(It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<int>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).Returns(AsyncYield(new[] { new SearchResult<EntityVectorRecord>(new EntityVectorRecord(), 1.0) }));

            _adapterMock.Setup(a => a.GetCollection<string, EntityVectorRecord>(It.IsAny<string>())).Returns(collectionMock.Object);
            _perfMock.Setup(p => p.StartOperation("Vector.Search", It.IsAny<IDictionary<string, object>>())).Returns(perfOp);

            var sut = new SkInMemoryVectorSearchService(_adapterMock.Object, _perfMock.Object);

            await sut.SearchAsync(new float[] { 1f }.AsMemory(), 2, new GenAIDBExplorer.Core.SemanticVectors.Infrastructure.VectorInfrastructure("col","prov"));

            _perfMock.Verify(p => p.StartOperation("Vector.Search", It.Is<IDictionary<string, object>>(d => d["Collection"].ToString() == "col" && (int)d["TopK"] == 2)), Times.Once);
        }
    }
}
