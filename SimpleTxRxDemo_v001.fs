\ \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
\
\ DESCRIPTION: this nRF52 Forth code demonstrates a simple transmitter node and a simple receiver node.  
\ Once every second the transmitter node will increment a counter and then transmit it.  For its part, 
\ the receiver node will immediately print whatever counters it receives.  As proof of correct 
\ operation, the received counter should match the transmitted counter.

\ DIRECTIONS: 
\ 1. Load the current versions of the following files:
\      i.   https://github.com/rabbithat/nRF52_delay_functions
\      ii.  https://github.com/rabbithat/nRF52_essential_definitions
\ 2. Only after loading the above files, load this file.
\ 3. At the REPL prompt, type 'tx' to create a transmitting node, or 'rx' to create a receiver node. 
\    For a proper demonstration, you will need one node of each type.

\
\ \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\

#251 CONSTANT _MAX_DATA_PAYLOAD_LENGTH  \ 251 bytes
\ #define MAX_PACKET_COUNTER_CHARACTERS 1
\ #define MAX_PAYLOAD_LENGTH (MAX_DATA_PAYLOAD_LENGTH + MAX_PACKET_COUNTER_CHARACTERS)  

\ Note: must be <= 255 and must be divisible evenly by 4 (for ease of memory addressing)  
\ 252 is the largest number of bytes to fit this criteria
#252 CONSTANT _MAX_PAYLOAD_LENGTH \ = MAX_DATA_PAYLOAD_LENGTH + MAX_PACKET_COUNTER_CHARACTERS

#256 CONSTANT _RADIO_BUFFER_SIZE
_RADIO_BUFFER_SIZE buffer: txRadioBuffer  \ this is buffer that the radio will for transmission
_RADIO_BUFFER_SIZE buffer: rxRadioBuffer  \ this is buffer that the radio will for receiving


$AA constant _target_prefixAddress \ prefix address of the other node
$DEADBEEF constant _target_baseAddress  \ base address of the other node

$AA constant _my_prefixAddress  \ prefix address of this node
$FEEDBEEF constant _my_baseAddress \ base address of this node

4 variable transmitterNode \ _TRUE iff the node is a transmitter node.  Otherwise, it's a receiver node.

: initializeSerialIo CR CR ." Starting..." CR CR ;

\ $40000000 constant _NRF_POWER
$40000578 constant _NRF_POWER__DCDCEN
: initializeHardware 1 _NRF_POWER__DCDCEN ! ;  \ enable the DCDC voltage regulator 

\ $40000000 CONSTANT NRF_CLOCK
$40000008 CONSTANT _NRF_CLOCK__LFCLKSTART
$4000000C CONSTANT _NRF_CLOCK__LFCLKSTOP
$40000104 CONSTANT _NRF_CLOCK__EVENTS_LFCLKSTARTED
$40000518 CONSTANT _NRF_CLOCK__LFCLKSRC \ should default to zero

  
\ 40000000 constant _NRF_CLOCK
$40000000 constant _NRF_CLOCK__TASKS_HFCLKSTART
$40000100 constant _NRF_CLOCK__EVENTS_HFCLKSTARTED
: initializeClocks 1 _NRF_CLOCK__TASKS_HFCLKSTART ! begin _NRF_CLOCK__EVENTS_HFCLKSTARTED @ 
until ;
  

\ 40001000 constant _NRF_RADIO
$40001508 constant _NRF_RADIO__FREQUENCY
$40001518 constant _NRF_RADIO__PCNF1
$40001514 constant _NRF_RADIO__PCNF0
$40001510 constant _NRF_RADIO__MODE
$40001650 constant _NRF_RADIO__MODECNF0
$40001534 constant _NRF_RADIO__CRCCNF
$40001504 constant _NRF_RADIO__PACKETPTR
$40001530 constant _NRF_RADIO__RXADDRESSES
$4000150C constant _NRF_RADIO__TXPOWER
 
 
: initializeRadio  #98 _NRF_RADIO__FREQUENCY !  $0004FF00 _NRF_RADIO__PCNF1 !  $00000800 _NRF_RADIO__PCNF0 ! #1 _NRF_RADIO__MODE ! #1 _NRF_RADIO__MODECNF0 ! #3 _NRF_RADIO__CRCCNF ! #1 _NRF_RADIO__RXADDRESSES ! #8 _NRF_RADIO__TXPOWER ! ;


$4000151C constant _NRF_RADIO__BASE0
$40001524 constant _NRF_RADIO__PREFIX0
$40001010 constant _NRF_RADIO__TASKS_DISABLE
$40001110 constant _NRF_RADIO__EVENTS_DISABLED
$40001004 constant _NRF_RADIO__TASKS_RXEN
$40001000 constant _NRF_RADIO__TASKS_TXEN
$40001100 constant _NRF_RADIO__EVENTS_READY
$40001008 constant _NRF_RADIO__TASKS_START
$4000110C constant _NRF_RADIO__EVENTS_END
  

: disableRadio  0 _NRF_RADIO__EVENTS_DISABLED !  1 _NRF_RADIO__TASKS_DISABLE ! begin     _NRF_RADIO__EVENTS_DISABLED @ until ;  
    
: activateRxidleState 0 _NRF_RADIO__EVENTS_READY !  1 _NRF_RADIO__TASKS_RXEN !  begin     _NRF_RADIO__EVENTS_READY  until ;  

: initializeRxIdleMode disableRadio  rxRadioBuffer _NRF_RADIO__PACKETPTR ! _target_baseAddress _NRF_RADIO__BASE0 ! _target_prefixAddress _NRF_RADIO__PREFIX0 ! activateRxidleState ;  
\ ASSERTION: now in RXIDLE state.  Ready to move into RX state.
  
  
\ turn on the radio receiver and shift into TXIDLE state
: activateTxidleState  0 _NRF_RADIO__EVENTS_READY !  1 _NRF_RADIO__TASKS_TXEN ! begin     _NRF_RADIO__EVENTS_READY @ until ;  

: initializeTxIdleMode disableRadio txRadioBuffer _NRF_RADIO__PACKETPTR ! _target_baseAddress _NRF_RADIO__BASE0 ! _target_prefixAddress _NRF_RADIO__PREFIX0 ! activateTxidleState ;  
\ ASSERTION: now in TXIDLE state.  Ready to move into TX state.

  

: guaranteeClear_EVENTS_END_semaphore  0 _NRF_RADIO__EVENTS_END !  begin _NRF_RADIO__EVENTS_END @  not until ;

  
: guaranteedTxOrRx 
  1 _NRF_RADIO__TASKS_START ! begin _NRF_RADIO__EVENTS_END @  until ;
  
: txOrRxBuffer guaranteeClear_EVENTS_END_semaphore guaranteedTxOrRx ;
  
  \ Note: this word is redundant but exists to improve code readability
  \ transmit from tx buffer and wait until radio is finished
: transmitTxBuffer txOrRxBuffer ;
  
  \ Note: this word is redundant but exists to improve code readability
  \ receive to rx buffer and wait until radio is finished
: receiveIntoRxBuffer txOrRxBuffer ;
  

: initializeEverything initializeSerialIo initializeHardware initializeClocks initializeRtc   initializeRadio ;

: repeatingTransmit   initializeTxIdleMode CR 0 begin 1+ dup txRadioBuffer ! transmitTxBuffer  dup . ." transmitted." CR #1000 delay_mSec again ;


: printReceivedPayload ." Payload received: " rxRadioBuffer @ . CR ;

: receive CR ." Listening..." CR initializeRxIdleMode begin receiveIntoRxBuffer printReceivedPayload again ;  

: tx   CR  ." I am a Transmitter node." CR  initializeEverything repeatingTransmit ;

: rx  CR  ." I am a Receiver node." CR  initializeEverything receive ;

