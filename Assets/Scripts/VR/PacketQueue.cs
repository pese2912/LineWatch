﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;


public class PacketQueue {

	//Packet Store Info
	struct PacketInfo
	{
		public int	offset;
		public int 	size;

	};

	private MemoryStream	m_streamBuffer;

	private List<PacketInfo>	m_offsetList;

	private int	m_offset = 0;

	private Object lockObj = new Object();

	//Constructor
	public PacketQueue()
	{
		m_streamBuffer = new MemoryStream ();
		m_offsetList = new List<PacketInfo> ();

	}

	//Enqueue
	public int Enqueue(byte[] data, int size)
	{
		PacketInfo info = new PacketInfo ();

		info.offset = m_offset;
		info.size = size;

		lock (lockObj) {
			//Packet info
			m_offsetList.Add (info);

			m_streamBuffer.Position = m_offset;
			m_streamBuffer.Write (data, 0, size);
			m_streamBuffer.Flush ();
			m_offset += size;

		}
		return size;
	}

	//Dequeue
	public int Dequeue(ref byte[] buffer, int size)
	{

		if (m_offsetList.Count <= 0) {
			return -1;
		}

		int recvSize = 0;

		lock (lockObj) {

			PacketInfo info = m_offsetList [0];

			//get Packet Data from buffer
			int dataSize = Math.Min (size, info.size);
			m_streamBuffer.Position = info.offset;
			recvSize = m_streamBuffer.Read (buffer, 0, dataSize);

			//first offset delete becauseof dequeue
			if (recvSize > 0) {
				m_offsetList.RemoveAt (0);
			}

			//stream clear if all data dequeue
			if (m_offsetList.Count == 0) {
				Clear ();
				m_offset = 0;
			}
		}
		return recvSize;
	}

	//Queue clear
	public void Clear()
	{
		byte[] buffer = m_streamBuffer.GetBuffer ();
		Array.Clear (buffer, 0, buffer.Length);

		m_streamBuffer.Position = 0;
		m_streamBuffer.SetLength (0);
	}


}
