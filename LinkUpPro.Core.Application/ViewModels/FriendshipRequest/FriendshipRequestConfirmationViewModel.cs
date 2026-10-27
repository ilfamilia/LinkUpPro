namespace LinkUpPro.Core.Application.ViewModels.FriendshipRequest
{
    public class FriendshipRequestConfirmationViewModel
    {
        public int RequestId { get; set; }
        public string Operation { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string ConfirmButtonText { get; set; } = string.Empty;
    }
}
