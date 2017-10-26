using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.CodePlex.Migration.WorkItems.Test
{
    public class CodePlexWorkItemReaderTests
    {
        private readonly HttpClientMock httpClientMock;
        private CodePlexWorkItemReader codePlexReader;

        public CodePlexWorkItemReaderTests()
        {
            httpClientMock = new HttpClientMock();
            codePlexReader = new CodePlexWorkItemReader("TestProject", true, httpClientMock.HttpClient.Object);
        }

        [Fact]
        public async Task GetWorkItems_SucceedsLessThanOnePage()
        {
            // Arrange
            PagedWorkItemList list = BuildPagedList(2);
            httpClientMock.Summaries = list;
            httpClientMock.SetupMock();

            // Act
            IReadOnlyList<WorkItemSummary> readerItems = await codePlexReader.GetWorkItemsAsync(x => true);

            // Assert
            VerifyWorkItemsReturnedAreSameAsSetOnMock(readerItems, list);
        }

        [Fact]
        public async Task GetWorkItems_SucceedsExactlyOnePage()
        {
            // Arrange
            PagedWorkItemList list = BuildPagedList(2);
            httpClientMock.Summaries = list;
            httpClientMock.PageSize = 2;
            httpClientMock.SetupMock();

            // Act
            IReadOnlyList<WorkItemSummary> readerItems = await codePlexReader.GetWorkItemsAsync(x => true);

            // Assert
            VerifyWorkItemsReturnedAreSameAsSetOnMock(readerItems, list);
        }

        [Fact]
        public async Task GetWorkItems_SucceedsMoreThanOnePage()
        {
            // Arrange
            PagedWorkItemList list = BuildPagedList(3);
            httpClientMock.Summaries = list;
            httpClientMock.PageSize = 2;
            httpClientMock.SetupMock();

            // Act
            IReadOnlyList<WorkItemSummary> readerItems = await codePlexReader.GetWorkItemsAsync(x => true);

            // Assert
            VerifyWorkItemsReturnedAreSameAsSetOnMock(readerItems, list);
        }

        [Fact]
        public async Task GetWorkItems_SucceedsMultipleOfPage()
        {
            // Arrange
            PagedWorkItemList list = BuildPagedList(6);
            httpClientMock.Summaries = list;
            httpClientMock.PageSize = 2;
            httpClientMock.SetupMock();

            // Act
            IReadOnlyList<WorkItemSummary> readerItems = await codePlexReader.GetWorkItemsAsync(x => true);

            // Assert
            VerifyWorkItemsReturnedAreSameAsSetOnMock(readerItems, list);
        }

        [Fact]
        public async Task GetWorkItems_PassesIncludeClosedWhenTrue()
        {
            // Arrange
            PagedWorkItemList list = BuildPagedList(6);
            httpClientMock.Summaries = list;
            httpClientMock.PageSize = 2;
            httpClientMock.SetupMock();

            // Act
            IReadOnlyList<WorkItemSummary> readerItems = await codePlexReader.GetWorkItemsAsync(x => true);

            // Assert
            httpClientMock.HttpClient.Verify(x => x.DownloadStringAsync("https://TestProject.codeplex.com/project/api/issues?showClosed=True"), Times.Once);
            httpClientMock.HttpClient.Verify(x => x.DownloadStringAsync("https://TestProject.codeplex.com/project/api/issues?start=2&showClosed=True"), Times.Once);
        }

        [Fact]
        public async Task GetWorkItems_PassesIncludeClosedWhenFalse()
        {
            // Arrange
            codePlexReader = new CodePlexWorkItemReader("TestProject", false, httpClientMock.HttpClient.Object);

            PagedWorkItemList list = BuildPagedList(6);
            httpClientMock.Summaries = list;
            httpClientMock.PageSize = 2;
            httpClientMock.SetupMock();

            // Act
            IReadOnlyList<WorkItemSummary> readerItems = await codePlexReader.GetWorkItemsAsync(x => true);

            // Assert
            httpClientMock.HttpClient.Verify(x => x.DownloadStringAsync("https://TestProject.codeplex.com/project/api/issues?showClosed=False"), Times.Once);
            httpClientMock.HttpClient.Verify(x => x.DownloadStringAsync("https://TestProject.codeplex.com/project/api/issues?start=2&showClosed=False"), Times.Once);
        }

        [Fact]
        public async Task GetWorkItems_DoesNotIncludeFilteredItems()
        {
            // Arrange
            PagedWorkItemList list = BuildPagedList(2);
            httpClientMock.Summaries = list;
            httpClientMock.PageSize = 2;
            httpClientMock.SetupMock();

            // Act
            IReadOnlyList<WorkItemSummary> readerItems = await codePlexReader.GetWorkItemsAsync(
                id =>
                {
                    if (id == 0)
                    {
                        return false;
                    }
                    return true;
                });

            // Assert
            Assert.True(readerItems.Count() == 1);

            WorkItemSummary item = readerItems.First();
            Assert.True(item.Id == 1);
        }

        [Fact]
        public async Task GetWorkItemAsync_ReturnsWorkItem()
        {
            // Arrange
            WorkItemSummary summary = new WorkItemSummary
            {
                Id = 1
            };

            httpClientMock.WorkItemDetail = new WorkItemDetails
            {
                FileAttachments = new List<WorkItemFileAttachment>(),
                Comments = new List<WorkItemComment>(),
                WorkItem = new WorkItem()
                {
                    Id = summary.Id
                },
                CanDeleteComments = true,
                CanDeleteWorkItem = true
            };
            httpClientMock.SetupMock();

            // Act
            WorkItemDetails details = await codePlexReader.GetWorkItemAsync(summary);

            // Assert

            // Spot check. Really the method should just be returning the serialized object from the endpoint 
            // so doing a deep check on the item would be a lot of verification of Newtonsoft.Json
            Assert.True(details.WorkItem.Id == summary.Id);
        }

        [Fact]
        public void GetWorkItemAsync_ThrowsArgumentExceptionIfWorkItemSummaryNull()
        {
            httpClientMock.SetupMock();
            Assert.ThrowsAsync<ArgumentNullException>(() => codePlexReader.GetWorkItemAsync(null));
        }

        private static PagedWorkItemList BuildPagedList(int numberofItems)
        {
            PagedWorkItemList pagedWorkItemListReturn = new PagedWorkItemList();
            var items =
                Enumerable.Range(0, numberofItems)
                   .Select(
                        i => new WorkItemSummary
                        {
                            Id = i,
                            Title = $"Some title for {i}"
                        });

            pagedWorkItemListReturn.WorkItemSummaries = items.ToList();
            pagedWorkItemListReturn.TotalItems = numberofItems;

            return pagedWorkItemListReturn;
        }

        private static void VerifyWorkItemsReturnedAreSameAsSetOnMock(IEnumerable<WorkItemSummary> workItems, PagedWorkItemList mockSetItems)
        {
            Assert.True(workItems.Count() == mockSetItems.TotalItems);

            foreach (WorkItemSummary summary in workItems)
            {
                WorkItemSummary sentItem = mockSetItems.WorkItemSummaries.Find(x => x.Id == summary.Id);

                Assert.NotNull(sentItem);
                Assert.Equal(sentItem.Title, summary.Title);
            }
        }
    }

    internal class HttpClientMock
    {
        private static readonly Regex WorkItemSummaryRequestCheck = 
            new Regex(@"^https://.*\.codeplex\.com/project/api/issues\?(start=(?<StartNumber>.*)&)?showClosed=.*$");

        public Mock<IHttpClient> HttpClient { get; }
        public PagedWorkItemList Summaries { get; set; }
        public WorkItemDetails WorkItemDetail { get; set; }
        public int PageSize { get; set; } = 100;
        public bool IsWorkItemSummaryRequest { get; } = true;

        public HttpClientMock()
        {
            HttpClient = new Mock<IHttpClient>();
        }

        public void SetupMock()
        {
            HttpClient
                .Setup(x => x.DownloadStringAsync(It.IsAny<string>()))
                .Returns((string uri) => DownloadStringAsync(uri));
        }

        public Task<string> DownloadStringAsync(string uri)
        {
            var match = WorkItemSummaryRequestCheck.Match(uri);

            if (match.Success && Summaries != null)
            {
                string startNumberValue = match.Groups["StartNumber"].Value;
                int startNumber = startNumberValue == "" ? 0 : Convert.ToInt32(startNumberValue);

                PagedWorkItemList returnList = new PagedWorkItemList
                {
                    TotalItems = Summaries.TotalItems
                };

                int countToGet = (startNumber + PageSize > Summaries.WorkItemSummaries.Count) ? Summaries.WorkItemSummaries.Count - startNumber : PageSize;

                returnList.WorkItemSummaries = Summaries.WorkItemSummaries.GetRange(startNumber, countToGet);

                return Task.FromResult(JsonConvert.SerializeObject(returnList));
            }
            else if (WorkItemDetail != null)
            {
                return Task.FromResult(JsonConvert.SerializeObject(WorkItemDetail));
            }

            throw new HttpRequestFailedException("Some reason to fail");
        }
    }
}
