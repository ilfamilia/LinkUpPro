using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Interfaces;
using LinkUpPro.Infrastructure.Persistence.Contexts;

namespace LinkUpPro.Infrastructure.Persistence.Repositories
{
    public class FriendshipRequestRepository : GenericRepository<FriendshipRequest>, IFriendshipRequestRepository
    {
        public FriendshipRequestRepository(LinkUpProAppContext context) : base(context)
        {
        }
    }
}
