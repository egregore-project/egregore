using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace egregore.Configuration
{
    public class DynamicControllerModelConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            if (!controller.ControllerType.IsGenericType)
                return;

            controller.ControllerName = controller.ControllerType.GenericTypeArguments[0].Name;
        }
    }
}
