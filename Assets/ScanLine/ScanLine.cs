using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering.PostProcessing
{

    [Serializable]
    [PostProcess(typeof(ScanLineRenderer), PostProcessEvent.AfterStack, "Alpacasking/ScanLine")]
    public sealed class ScanLine : PostProcessEffectSettings
    {
        [Range(0f, 100f)]
        public FloatParameter DepthEdgeStrength = new FloatParameter { value = 40f };

        [Range(0f, 100f)]
        public FloatParameter DistanceEdgeStrength = new FloatParameter { value = 1f };

        [Range(1f, 5f)]
        public FloatParameter DistanceScale = new FloatParameter { value = 1f };

        [Range(1f, 1000f)]
        public FloatParameter DistanceThresold = new FloatParameter { value = 100f };

        public Vector3Parameter Target = new Vector3Parameter { value = Vector3.zero};

        public ColorParameter DepthEdgeColor = new ColorParameter { value = new Color(0.3964934f, 0.6966289f, 0.8490566f) };

        public ColorParameter DistanceEdgeColor = new ColorParameter { value = new Color(0.8744395f, 0.9245283f, 0.4579031f) };

        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled.value;
               
        }
    }


    [UnityEngine.Scripting.Preserve]
    public sealed class ScanLineRenderer : PostProcessEffectRenderer<ScanLine>
    {
        private ComputeShader depthEdgeShader;

        private ComputeShader distanceFieldShader;

        private ComputeShader colorMixerShader;

        private int distanceFromTargetKernelID;

        private int sobelForDepthKernelID;
        private int sobelForDisatnceKernelID;
        private int colroMixerKernelID;
        private int tempRTID1;

        private int tempRTID2;

        private int tempRTID3;


        public override void Init()
        {
            distanceFieldShader = (ComputeShader)Resources.Load("DistanceField");
            depthEdgeShader = (ComputeShader)Resources.Load("DepthEdge");
            colorMixerShader = (ComputeShader)Resources.Load("ColorMixer");
            sobelForDepthKernelID = depthEdgeShader.FindKernel("SobelForDepth");
            sobelForDisatnceKernelID = depthEdgeShader.FindKernel("SobelForDistance");
            distanceFromTargetKernelID = distanceFieldShader.FindKernel("DistanceFromTarget");
            colroMixerKernelID = colorMixerShader.FindKernel("ColorMixer");
            tempRTID1 = Shader.PropertyToID("tempRTID1");
            tempRTID2 = Shader.PropertyToID("tempRTID2");
            tempRTID3 = Shader.PropertyToID("tempRTID3");

        }
        public override void Release()
        {

        }

        public override void Render(PostProcessRenderContext context)
        {
           
            var cmd = context.command;
            cmd.BeginSample("ScanLine");
            RenderTextureDescriptor desc = new RenderTextureDescriptor(context.width, context.height);
            desc.enableRandomWrite = true;
            //cmd.GetTemporaryRT(tempCanvas, desc);
            desc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;
            cmd.GetTemporaryRT(tempRTID1, desc);
            cmd.GetTemporaryRT(tempRTID2, desc);
            cmd.GetTemporaryRT(tempRTID3, desc);

    
            Matrix4x4 V = Camera.main.worldToCameraMatrix;
            Matrix4x4 P = GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, true);
            Matrix4x4 VP = P* V ;
            Matrix4x4 InvMVP = VP.inverse;

            // depth edge
            cmd.SetComputeIntParam(depthEdgeShader, "Width", context.width - 1);
            cmd.SetComputeIntParam(depthEdgeShader, "Height", context.height - 1);
  
            cmd.SetComputeFloatParam(depthEdgeShader, "EdgeStrength", settings.DepthEdgeStrength.value);

            cmd.SetComputeTextureParam(depthEdgeShader, sobelForDepthKernelID, "Source", BuiltinRenderTextureType.Depth);// or ResolvedDepth
            cmd.SetComputeTextureParam(depthEdgeShader, sobelForDepthKernelID, "Result", tempRTID1);
            cmd.DispatchCompute(depthEdgeShader, sobelForDepthKernelID, context.width, context.height, 1);


            // distance field
            cmd.SetComputeIntParam(distanceFieldShader, "Width", context.width - 1);
            cmd.SetComputeIntParam(distanceFieldShader, "Height", context.height - 1);
            cmd.SetComputeFloatParam(distanceFieldShader, "DistanceScale", settings.DistanceScale.value);
            cmd.SetComputeMatrixParam(distanceFieldShader, "InvVPMatrix", InvMVP);
            var tar = settings.Target.value;
            cmd.SetComputeFloatParams(distanceFieldShader, "Target", new float[3] { tar.x,tar.y,tar.z});
            cmd.SetComputeTextureParam(distanceFieldShader, distanceFromTargetKernelID, "Source", BuiltinRenderTextureType.Depth);// or ResolvedDepth
            cmd.SetComputeTextureParam(distanceFieldShader, distanceFromTargetKernelID, "Result", tempRTID2);
            cmd.DispatchCompute(distanceFieldShader, distanceFromTargetKernelID, context.width, context.height, 1);

            // distance field edge
            cmd.SetComputeFloatParam(depthEdgeShader, "EdgeStrength", settings.DistanceEdgeStrength.value);
            cmd.SetComputeFloatParam(depthEdgeShader, "DistanceThresold", settings.DistanceThresold.value);
            cmd.SetComputeTextureParam(depthEdgeShader, sobelForDisatnceKernelID, "Source", tempRTID2);// or ResolvedDepth
            cmd.SetComputeTextureParam(depthEdgeShader, sobelForDisatnceKernelID, "Result", tempRTID3);
            cmd.DispatchCompute(depthEdgeShader, sobelForDisatnceKernelID, context.width, context.height, 1);

            // mix color
            var depthColor = settings.DepthEdgeColor.value;
            cmd.SetComputeFloatParams(colorMixerShader, "DepthEdgeColor", new float[3] { depthColor.r, depthColor.g, depthColor.b });
            var distanceColor = settings.DistanceEdgeColor.value;
            cmd.SetComputeFloatParams(colorMixerShader, "DistanceEdgeColor", new float[3] { distanceColor.r, distanceColor.g, distanceColor.b });

            cmd.SetComputeTextureParam(colorMixerShader, colroMixerKernelID, "ColorSource", context.source);
            cmd.SetComputeTextureParam(colorMixerShader, colroMixerKernelID, "DepthSource", tempRTID1);
            cmd.SetComputeTextureParam(colorMixerShader, colroMixerKernelID, "DistanceSource", tempRTID3);

            cmd.SetComputeTextureParam(colorMixerShader, colroMixerKernelID, "Result", tempRTID2);
            cmd.DispatchCompute(colorMixerShader, colroMixerKernelID, context.width, context.height, 1);

            cmd.Blit(tempRTID2, context.destination);
            cmd.ReleaseTemporaryRT(tempRTID1);
            cmd.ReleaseTemporaryRT(tempRTID2);
            cmd.ReleaseTemporaryRT(tempRTID3);

            cmd.EndSample("ScanLine");
        }
    }

}

