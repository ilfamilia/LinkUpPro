using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace LinkUpPro.Core.Application
{
    public static class ServicesRegistration
    {
        public static void AddApplicationLayerIoc(this IServiceCollection services)
        {
            #region Configurations
            services.AddAutoMapper(cfg => { }, Assembly.GetExecutingAssembly());
            #endregion

            #region Services IOC
            services.AddScoped<ICommentService, CommentService>();
            services.AddScoped<IPostService, PostService>();
            services.AddScoped<IReactionService, ReactionService>();
            services.AddScoped<IFriendshipRequestService, FriendshipRequestService>();
            services.AddScoped<IFriendshipService, FriendshipService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IBattleshipGameService, BattleshipGameService>();
            services.AddScoped<IBattleshipPlacementService, BattleshipPlacementService>();
            services.AddScoped<IBattleshipAttackService, BattleshipAttackService>();
            #endregion
        }
    }
}