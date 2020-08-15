using System.Threading.Tasks;
using egregore.Configuration;
using egregore.Models;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace egregore.Filters
{
    public class BaseViewModelFilter : IAsyncActionFilter
    {
        private readonly IOptionsSnapshot<WebServerOptions> _options;

        public BaseViewModelFilter(IOptionsSnapshot<WebServerOptions> options)
        {
            _options = options;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach(var argument in context.ActionArguments)
            {
                if(argument.Value is BaseViewModel model)
                {
                    model.ServerId = _options.Value.ServerId;
                }
            }

            await next();
        }
    }
}
