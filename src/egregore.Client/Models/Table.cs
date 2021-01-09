using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace egregore.Client.Models
{
    public sealed class Table : ComponentBase, IDisposable
    {
        public Table()
        {
            
        }

        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenElement(0, HtmlTags.Table);
            b.AddAttribute(1, HtmlAttributes.Class, "w-auto");
            b.CloseElement();
        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            return base.OnAfterRenderAsync(firstRender);
        }
        
        protected override Task OnInitializedAsync()
        {
            return base.OnInitializedAsync();
        }

        protected override Task OnParametersSetAsync()
        {
            return base.OnParametersSetAsync();
        }

        public void Dispose() { }
    }
}
