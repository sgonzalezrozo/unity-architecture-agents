using System.Threading.Tasks;
using Xunit;

namespace UnityArchitecture.Analyzers.Tests
{
    public class ArchitectureAnalyzerTests
    {
        private const string USINGS = @"
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UIFoundations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
";

        private const string FAST_IOC = "namespace FastIoC.API { public abstract class BindingRecorder { } } ";

        [Theory]
        [InlineData("UA0001", "public class ShopView : MonoBehaviour { } public class ShopModel { private ShopView view; }")]
        [InlineData("UA0001", "public class ShopController { } public class ShopModel { private List<ShopController> items; }")]
        [InlineData("UA0001", "public class ShopModel { private Transform root; }")]
        [InlineData("UA0001", "public class ItemView : MonoBehaviour { } public class ShopView : MonoBehaviour { private ItemView item; }")]
        [InlineData("UA0001", "public class ShopView : MonoBehaviour { } public class ShopController : MonoBehaviour { private ShopView view; }")]
        [InlineData("UA0001", "public class ShopView : MonoBehaviour { } public class ShopModel { public void Set(ShopView view) { } }")]
        [InlineData("UA0002", "public class ShopView : MonoBehaviour, IPointerClickHandler { }")]
        [InlineData("UA0003", "public class ShopController : MonoBehaviour { [SerializeField] private Image image; }")]
        [InlineData("UA0004", "public class ShopModel { public event Action<int> OnChanged; }")]
        [InlineData("UA0005", "public class Screen : MonoBehaviour { private Button button; }")]
        [InlineData("UA0005", "public class Screen : MonoBehaviour { private Toggle toggle; }")]
        [InlineData("UA0006", "public class ShopView : MonoBehaviour { public void Initialize(int count) { } }")]
        [InlineData("UA0007", FAST_IOC + "public class InjectAttribute : Attribute { } public class Screen : MonoBehaviour { [Inject] internal int value; }")]
        [InlineData("UA0008", "public class Screen : MonoBehaviour { public int value; }")]
        [InlineData("UA0008", FAST_IOC + "public class Screen : MonoBehaviour { [SerializeField] internal int value; }")]
        [InlineData("UA0009", "public class Shop { private void Run() { Action action = () => { }; } }")]
        [InlineData("UA0009", "public class Shop { private void Run() { Action action = delegate { }; } }")]
        [InlineData("UA0010", "public class Shop { private bool Run(List<int> items) { return items.Any(); } }")]
        [InlineData("UA0011", "public class Shop { private void Run() { var count = 1; } }")]
        [InlineData("UA0011", "public class Shop { private void Run(List<int> items) { foreach (var item in items) { } } }")]
        [InlineData("UA0012", "public class Shop { private void Run(Transform root) { root?.ToString(); } }")]
        public async Task ReportsRule(string rule, string source)
        {
            string[] rules = await AnalyzerHarness.AnalyzeAsync(USINGS + source);

            Assert.Contains(rule, rules);
        }

        [Theory]
        [InlineData("public class ShopModel { public event Action OnChanged; public int Count => 1; }")]
        [InlineData("public class ItemModel { } public class ShopModel { private ItemModel item; }")]
        [InlineData("public class CellModel : MonoBehaviour { } public class ShopModel : MonoBehaviour { private CellModel cell; }")]
        [InlineData("public class ShopModel { } public class ShopView : MonoBehaviour { internal ShopModel model; [SerializeField] private Image image; }")]
        [InlineData("public class ShopModel { } public class BuyController : BaseClickController { internal ShopModel model; [SerializeField] private int amount; }")]
        [InlineData("public class Form : MonoBehaviour { private TMP_InputField input; }")]
        [InlineData("public class Shop : MonoBehaviour { public const int MAX = 3; public static int Count; }")]
        [InlineData("public class Shop { public void Initialize(int count) { } }")]
        [InlineData("public class InjectAttribute : Attribute { } public class Screen : MonoBehaviour { [Inject] internal int value; }")]
        [InlineData("public class Screen : MonoBehaviour { [SerializeField] internal int value; }")]
        [InlineData("public class Shop { private void Run(List<int> items) { for (int i = 0; i < items.Count; i++) { } } }")]
        [InlineData("public class Shop { private void Run(string text) { int? length = text?.Length; } }")]
        public async Task AllowsValidCode(string source)
        {
            string[] rules = await AnalyzerHarness.AnalyzeAsync(USINGS + source);

            Assert.Empty(rules);
        }
    }
}
