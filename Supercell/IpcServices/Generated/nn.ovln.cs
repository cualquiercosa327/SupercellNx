#pragma warning disable 169, 465
using System;
using static Supercell.Globals;
namespace Supercell.IpcServices.Nn.Ovln {
	public unsafe partial class IReceiver : _Base_IReceiver {}
	public unsafe class _Base_IReceiver : IpcInterface {
		public void Dispatch(IncomingMessage im, OutgoingMessage om) {
			switch(im.CommandId) {
				case 0: { // Unknown0
					Unknown0(null);
					break;
				}
				case 1: { // Unknown1
					Unknown1(null);
					break;
				}
				case 2: { // Unknown2
					var ret = Unknown2();
					om.Copy(0, ret.Handle);
					break;
				}
				case 3: { // Unknown3
					var ret = Unknown3();
					break;
				}
				case 4: { // Unknown4
					var ret = Unknown4();
					break;
				}
				default:
					throw new NotImplementedException($"Unhandled command ID to IReceiver: {im.CommandId}");
			}
		}
		
		public virtual void Unknown0(object _0) => throw new NotImplementedException();
		public virtual void Unknown1(object _0) => throw new NotImplementedException();
		public virtual KObject Unknown2() => throw new NotImplementedException();
		public virtual object Unknown3() => throw new NotImplementedException();
		public virtual object Unknown4() => throw new NotImplementedException();
	}
	
	public unsafe partial class IReceiverService : _Base_IReceiverService {}
	public unsafe class _Base_IReceiverService : IpcInterface {
		public void Dispatch(IncomingMessage im, OutgoingMessage om) {
			switch(im.CommandId) {
				case 0: { // Unknown0
					var ret = Unknown0();
					om.Move(0, ret.Handle);
					break;
				}
				default:
					throw new NotImplementedException($"Unhandled command ID to IReceiverService: {im.CommandId}");
			}
		}
		
		public virtual Nn.Ovln.IReceiver Unknown0() => throw new NotImplementedException();
	}
	
	public unsafe partial class ISender : _Base_ISender {}
	public unsafe class _Base_ISender : IpcInterface {
		public void Dispatch(IncomingMessage im, OutgoingMessage om) {
			switch(im.CommandId) {
				case 0: { // Unknown0
					Unknown0(null);
					break;
				}
				case 1: { // Unknown1
					var ret = Unknown1();
					break;
				}
				default:
					throw new NotImplementedException($"Unhandled command ID to ISender: {im.CommandId}");
			}
		}
		
		public virtual void Unknown0(object _0) => throw new NotImplementedException();
		public virtual object Unknown1() => throw new NotImplementedException();
	}
	
	public unsafe partial class ISenderService : _Base_ISenderService {}
	public unsafe class _Base_ISenderService : IpcInterface {
		public void Dispatch(IncomingMessage im, OutgoingMessage om) {
			switch(im.CommandId) {
				case 0: { // Unknown0
					var ret = Unknown0(null);
					om.Move(0, ret.Handle);
					break;
				}
				default:
					throw new NotImplementedException($"Unhandled command ID to ISenderService: {im.CommandId}");
			}
		}
		
		public virtual Nn.Ovln.ISender Unknown0(object _0) => throw new NotImplementedException();
	}
}