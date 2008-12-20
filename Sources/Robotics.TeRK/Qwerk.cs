﻿// AForge TeRK Robotics Library
// AForge.NET framework
//
// Copyright © Andrew Kirillov, 2007-2008
// andrew.kirillov@aforgenet.com
//

using TeRKIceLib = TeRK;

namespace AForge.Robotics.TeRK
{
    using System;

    /// <summary>
    /// Manipulation of Qwerk robotics board.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>The class allows to manipulate with Qwerk robotics board developed by
    /// <a href="http://www.charmedlabs.com/">Charmed Labs</a> and supported by
    /// <a href="http://www.terk.ri.cmu.edu/">TeRK project</a>. Using this class it is
    /// possible to get access to different Qwerk's services, like digital inputs and outputs, 
    /// motors and servos, analog inputs, on-board LEDs, video camera, etc.</para>
    /// 
    /// <para><img src="img/robotics/qwerk_board.jpg" width="192" height="201" /></para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// Qwerk qwerk = new Qwerk( );
    /// // connect to Qwerk
    /// qwerk.Connect( "192.168.0.5" );
    /// // turn off all on-board LEDs
    /// qwerk.GetLedsService( ).SetLedsState( Qwerk.LedState.Off );
    /// // get digital output service
    /// Qwerk.DigitalOut outputs = qwerk.GetDigitalOutService( );
    /// // set all digital outputs to disabled state
    /// outputs.SetOutputs( false );
    /// // enable zero output
    /// outputs.SetOutput( 0, true );
    /// </code>
    /// </remarks>
    /// 
    public partial class Qwerk
    {
        private Ice.Communicator iceCommunicator = null;
        private TeRKIceLib.QwerkPrx qwerk = null;

        // host address if connection was established
        private string hostAddress;

        /// <summary>
        /// Qwerk's host address.
        /// </summary>
        /// 
        /// <remarks><para>The property keeps Qwerk's host address if the class is connected
        /// to Qwerk board, otherwise it equals to <see langword="null."/>.</para></remarks>
        ///
        public string HostAddress
        {
            get { return hostAddress; }
        }

        /// <summary>
        /// Connection state.
        /// </summary>
        /// 
        /// <remarks><para>The property equals to <see langword="true"/> if the class is connected
        /// to Qwerk board, otherwise it equals to <see langword="false"/>.</para></remarks>
        /// 
        public bool IsConnected
        {
            get { return ( hostAddress != null ); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Qwerk"/> class.
        /// </summary>
        /// 
        public Qwerk( )
        {
        }

        /// <summary>
        /// Connect to Qwerk.
        /// </summary>
        /// 
        /// <param name="hostAddress">Qwerk's address or host name to connect to.</param>
        /// 
        /// <exception cref="ConnectFailedException">Failed connecting to Qwerk.</exception>
        /// <exception cref="ServiceAccessFailedException">Failed accessing to the requested service,
        /// which may be due to the fact that something is wrong with Qwerk device or connection
        /// was initiated not with Qwerk.</exception>
        /// 
        public void Connect( string hostAddress )
        {
            // close previous connection
            Disconnect( );

            try
            {
                // initialize ICE communication
                iceCommunicator = Ice.Util.initialize( );

                // get Qwerk object
                Ice.ObjectPrx obj = iceCommunicator.stringToProxy( "'::TeRK::TerkUser':tcp -h " + hostAddress + " -p 10101" );
                qwerk = TeRKIceLib.QwerkPrxHelper.checkedCast( obj );

            }
            catch
            {
                Disconnect( );

                throw new ConnectFailedException( "Failed connecting to the requested service." );
            }

            // check if qwerk's object was obtained successfully
            if ( qwerk == null )
            {
                Disconnect( );

                throw new ServiceAccessFailedException( "Failed accessing to the requested service." );
            }
            else
            {
                // save host address
                this.hostAddress = hostAddress;
            }
        }

        /// <summary>
        /// Disconnect from Qwerk device.
        /// </summary>
        /// 
        public void Disconnect( )
        {
            hostAddress = null;

            if ( video != null )
            {
                video.SignalToStop( );
                // wait for aroung 250 ms
                for ( int i = 0; ( i < 5 ) && ( video.IsRunning ); i++ )
                {
                    System.Threading.Thread.Sleep( 50 );
                }
                // abort camera if it can not be stopped
                if ( video.IsRunning )
                {
                    video.Stop( );
                }
            }

            leds = null;
            digitalOuts = null;
            video = null;

            // destroy ICE communicator
            if ( iceCommunicator != null )
            {
                iceCommunicator.destroy( );
                iceCommunicator = null;
            }
        }

        private Qwerk.Leds leds;

        /// <summary>
        /// Get Qwerk's LEDs service.
        /// </summary>
        /// 
        /// <returns>Returns Qwerk's LEDs service.</returns>
        /// 
        /// <remarks>For the list of possible exceptions, see documentation to
        /// <see cref="Qwerk.Leds(Qwerk)"/>.</remarks>
        /// 
        public Qwerk.Leds GetLedsService( )
        {
            if ( leds == null )
            {
                leds = new Leds( this );
            }
            return leds;
        }

        private Qwerk.DigitalOut digitalOuts;

        /// <summary>
        /// Get Qwerk's digital outputs service.
        /// </summary>
        /// 
        /// <returns>Returns Qwerk's digital outputs service.</returns>
        /// 
        /// <remarks>For the list of possible exceptions, see documentation to
        /// <see cref="Qwerk.DigitalOut(Qwerk)"/>.</remarks>
        /// 
        public Qwerk.DigitalOut GetDigitalOutService( )
        {
            if ( digitalOuts == null )
            {
                digitalOuts = new DigitalOut( this );
            }
            return digitalOuts;
        }

        private Qwerk.Video video;

        /// <summary>
        /// Get Qwerk's video service.
        /// </summary>
        /// 
        /// <returns>Returns Qwerk's video service.</returns>
        /// 
        /// <remarks>For the list of possible exceptions, see documentation to
        /// <see cref="Qwerk.Video(Qwerk)"/>.</remarks>
        /// 
        public Qwerk.Video GetVideoService( )
        {
            if ( video == null )
            {
                video = new Video( this );
            }
            return video;
        }

        /// <summary>
        /// Get Qwerk's power level.
        /// </summary>
        /// 
        /// <returns>Returns Qwerk's power level in millivolts.</returns>
        /// 
        /// <exception cref="ConnectionLostException">Connestion to Qwerk is lost.</exception>
        /// 
        public int GetPower( )
        {
            int power = 0;
            try
            {
                TeRKIceLib.QwerkState state = qwerk.getState( );
                power = state.battery.batteryVoltage;
            }
            catch
            {
                throw new ConnectionLostException( "Connection is lost." );
            }
            return power;
        }
    }
}
