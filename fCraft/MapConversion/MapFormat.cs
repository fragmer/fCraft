// 
//  Authors:
//   *  Tyler Kennedy <tk@tkte.ch>
//   *  Matvei Stefarov <fragmer@gmail.com>
// 
//  Copyright (c) 2010-2011, Tyler Kennedy & Matvei Stefarov
// 
//  All rights reserved.
// 
//  Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
// 
//     * Redistributions of source code must retain the above copyright notice, this
//       list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice,
//       this list of conditions and the following disclaimer in
//       the documentation and/or other materials provided with the distribution.
//     * Neither the name of MCC nor the names of its contributors may be
//       used to endorse or promote products derived from this software without
//       specific prior written permission.
// 
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
//  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
//  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
//  A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
//  CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
//  EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
//  PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
//  PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
//  LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//  NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//  SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 


namespace fCraft.MapConversion {
    // <summary>
    // Enum defining all of the MapFormats supported by mcc
    // </summary>
    public enum MapFormat {
        FCMv2,
        // Map format used by fCraft.
        // - Support added by Matvei
        FCMv3,
        // Future map format used by fCraft
        // - Support added by Matvei
        MCSharp,
        // Map format used by MCSharp
        // - Support added by Tyler
        MinerCPP,
        // Map format used by MinerCPP and LuaCraft
        // - Support added by Tyler
        Myne,
        // Map format used by Myne and its children (Mainly Hyvebuild)
        // - Support added by Matvei
        Creative,
        // Creative makes more sense then the original "Vanilla"
        // - Support added by Tyler
        NBT,
        // Format used by indev and infdev
        // - Support added by Matvei
        JTE,
        // Format used by JTE's server
        // - Support added by Matvei
        D3,
        // Format used by D3 server
        // - Support added by Matvei
        Unknown
        // Returned when the map could not be identified
    }

    public enum MapFormatType {
        SingleFile,
        Directory
    }
}
