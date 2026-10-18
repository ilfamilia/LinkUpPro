namespace LinkUpPro.Core.Application.Dtos.Common
{
    public class BasicDto<TKey>
    {
        public required TKey Id { get; set; }
    }
}
