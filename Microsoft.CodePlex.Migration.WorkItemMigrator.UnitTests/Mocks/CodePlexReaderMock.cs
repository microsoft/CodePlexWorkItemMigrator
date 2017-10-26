using System;
using System.Collections.Generic;
using System.Linq;
using Moq;

namespace Microsoft.CodePlex.Migration.WorkItems.Test
{
    internal class CodePlexReaderMock
    {
        public List<WorkItemSummary> Summaries { get; set; }
        public List<WorkItemDetails> WorkItems { get; set; }

        public readonly Mock<IWorkItemSource> mockReader;

        public CodePlexReaderMock()
        {
            mockReader = new Mock<IWorkItemSource>();
        }

        /// <summary>
        /// Sets up the mock.
        /// </summary>
        /// <remarks>
        /// This needs to be called after Summaries and WorkItems are set.
        /// Also note.  this is called from BuildWorkItemLists so if you are using that to populate the lists you do not need to call this method.
        /// </remarks>
        public void SetupMock()
        { 
            mockReader.Setup(m => m.GetWorkItemsAsync(It.IsAny<Func<int, bool>>()))
                .ReturnsAsync((Func<int, bool> predicate) => Summaries.Where(w => predicate(w.Id)).ToArray());
            mockReader.Setup(m => m.GetWorkItemAsync(It.IsAny<WorkItemSummary>()))
                .ReturnsAsync(
                   (WorkItemSummary x) =>
                   {
                       foreach (var detail in WorkItems)
                       {
                           if (detail.WorkItem.Id == x.Id)
                           {
                               return detail;
                           }
                       }

                       throw new ArgumentException("Failed to find the work item");
                   });
        }

        public void BuildWorkItemLists(int numberOfWorkItems)
        {
            Summaries = new List<WorkItemSummary>();
            WorkItems = new List<WorkItemDetails>();

            for (int i = 0; i < numberOfWorkItems; i++)
            {
                WorkItemSummary newSummary = new WorkItemSummary
                {
                    Id = i
                };

                Summaries.Add(newSummary);

                WorkItemDetails newDetails = new WorkItemDetails
                {
                    WorkItem = new WorkItem
                    {
                        Id = i
                    }
                };

                WorkItems.Add(newDetails);
            }

            SetupMock();
        }

        public void VerifyReaderCalledForWorkItems()
        {
            mockReader.Verify(m => m.GetWorkItemsAsync(It.IsAny<Func<int, bool>>()), Times.Once);
            foreach (WorkItemSummary summary in Summaries)
            {
                mockReader.Verify(m => m.GetWorkItemAsync(It.Is<WorkItemSummary>(x => x == summary)), Times.Once);
            }
        }
    }
}
