using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace BibliotecaAPI.Swagger
{
    public class GroupByVersionConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            // Ejemplo: "Controllers.V1"
            var controllerNamespace = controller.ControllerType.Namespace;
            var version = controllerNamespace!.Split(".").Last().ToLower();
            controller.ApiExplorer.GroupName = version;
        }
    }
}
