using NUnit.Framework;
using UnityEngine;

namespace StrangeToolkit.Tests
{
    // ADR 0002: the editor's fog/clipping rules (mirrored in StrangeHub.ApplyAtmosphere)
    public class AtmosphereMathTests
    {
        [Test]
        public void NearClipIsClampedToVrchatRange()
        {
            Assert.AreEqual(0.001f, AtmosphereMath.ClampNear(0f));
            Assert.AreEqual(0.05f, AtmosphereMath.ClampNear(0.3f));
            Assert.AreEqual(0.01f, AtmosphereMath.ClampNear(0.01f));
        }

        [Test]
        public void FarClipStaysAtLeastPointOneAboveNear()
        {
            Assert.AreEqual(0.15f, AtmosphereMath.ClampFar(0f, 0.05f), 1e-5f);
            Assert.AreEqual(500f, AtmosphereMath.ClampFar(500f, 0.01f));
        }

        [Test]
        public void LinearFogHidesAtEndDistance()
        {
            Assert.AreEqual(300f, AtmosphereMath.FogHiddenDistance(true, FogMode.Linear, 0f, 10f, 300f));
            Assert.AreEqual(-1f, AtmosphereMath.FogHiddenDistance(true, FogMode.Linear, 0f, 300f, 10f));
        }

        [Test]
        public void ExponentialFogHidesAtNinetyNinePercent()
        {
            // exp(-d * 0.01) = 0.01  =>  d = ln(100) / 0.01
            Assert.AreEqual(460.517f, AtmosphereMath.FogHiddenDistance(true, FogMode.Exponential, 0.01f, 0f, 0f), 0.01f);
            // exp(-(d * 0.01)^2) = 0.01  =>  d = sqrt(ln(100)) / 0.01
            Assert.AreEqual(214.597f, AtmosphereMath.FogHiddenDistance(true, FogMode.ExponentialSquared, 0.01f, 0f, 0f), 0.01f);
        }

        [Test]
        public void NoFogMeansNoHiddenDistance()
        {
            Assert.AreEqual(-1f, AtmosphereMath.FogHiddenDistance(false, FogMode.Exponential, 0.01f, 0f, 0f));
            Assert.AreEqual(-1f, AtmosphereMath.FogHiddenDistance(true, FogMode.Exponential, 0f, 0f, 0f));
        }

        [Test]
        public void FogModeZeroKeepsSceneMode()
        {
            Assert.AreEqual(FogMode.ExponentialSquared, AtmosphereMath.EffectiveFogMode(0, FogMode.ExponentialSquared));
            Assert.AreEqual(FogMode.Linear, AtmosphereMath.EffectiveFogMode(1, FogMode.ExponentialSquared));
        }

        [Test]
        public void FarClipComparedToFog()
        {
            Assert.AreEqual(AtmosphereMath.FarClipFit.NoFog, AtmosphereMath.CompareFarClipToFog(1000f, -1f));
            Assert.AreEqual(AtmosphereMath.FarClipFit.CutsBeforeFog, AtmosphereMath.CompareFarClipToFog(200f, 300f));
            Assert.AreEqual(AtmosphereMath.FarClipFit.Fine, AtmosphereMath.CompareFarClipToFog(300f, 300f));
            Assert.AreEqual(AtmosphereMath.FarClipFit.RendersPastFog, AtmosphereMath.CompareFarClipToFog(1000f, 300f));
        }
    }
}
