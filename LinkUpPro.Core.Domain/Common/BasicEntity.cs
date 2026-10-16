namespace LinkUpPro.Core.Domain.Common
{
    public class BasicEntity<TKey>
    {
        public required TKey Id { get; set; }
    }
}
