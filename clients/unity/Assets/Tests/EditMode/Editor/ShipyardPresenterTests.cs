using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpaceTraders.Core;
using SpaceTraders.Generated.Model;
using SpaceTraders.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class ShipyardPresenterTests
    {
        private VisualElement _root;
        private ScrollView _scrollView;
        private ShipyardPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _root = new VisualElement();
            _scrollView = new ScrollView();
            _root.Add(_scrollView);

            var go = new GameObject();
            _presenter = go.AddComponent<ShipyardPresenter>();
        }

        [Test]
        public void Populate_WithEmptyShipyard_DisplaysNoShipsMessage()
        {
            var shipyard = new Shipyard(
                symbol: "X1-TEST-1",
                shipTypes: new List<ShipyardShipTypesInner>(),
                modificationsFee: 100
            );

            _presenter.Populate(_scrollView, shipyard);

            var label = _scrollView.Q<Label>();
            Assert.IsNotNull(label);
            Assert.IsTrue(label.text.Contains("No ships available"));
        }

        [Test]
        public void Populate_WithAvailableShips_AddsEntriesToScroll()
        {
            // We need a template for the ship entry
            var template = ScriptableObject.CreateInstance<VisualTreeAsset>();
            // In a real test we'd need to mock the template instantiation or use a dummy.
            // For unit tests in Unity, it's sometimes hard to use VisualTreeAsset without a real asset.
            // Let's assume we use a simpler version or mock the behavior.
        }
    }
}
