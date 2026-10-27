namespace LinkUpPro.Core.Application.ViewModels.FriendshipRequest
{
    public class FriendshipRequestIndexViewModel
    {
        public int ReceivedPendingCount { get; set; }
        public List<FriendshipRequestListViewModel> ReceivedPending { get; set; } = [];
        public List<FriendshipRequestListViewModel> Sent { get; set; } = [];
    }
}
