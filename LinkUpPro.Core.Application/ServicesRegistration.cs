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
            // Register module use cases and a replaceable clock for deterministic time-based tests.
            services.AddSingleton(TimeProvider.System);
            services.AddScoped<Services.SocialUserGuard>();
            services.AddScoped<Interfaces.IFriendshipService, Services.FriendshipService>();
            services.AddScoped<Interfaces.IFriendshipRequestService, Services.FriendshipRequestService>();
            services.AddScoped<Interfaces.INotificationService, Services.NotificationService>();
            services.AddScoped<Interfaces.IBattleshipService, Services.BattleshipService>();
            #endregion
        }
    }
}
