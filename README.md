# nRF52_SimpleTxRxDemo
Simple demo code of one node transmitting a counter and the other node receiving the counter that was sent

DESCRIPTION: this nRF52 Forth code demonstrates a simple transmitter node and a simple receiver node.  
Once every second the transmitter node will increment a counter and then transmit it.  For its part 
the receiver node will immediately print whatever counters it receives.  As proof of correct 
operation, the received counter should match the transmitted counter.

DIRECTIONS: 
  1. Load the current versions of the following files:
  
    i.   https://github.com/rabbithat/nRF52_delay_functions
    
    ii.  https://github.com/rabbithat/nRF52_essential_definitions
    
  2. Only after loading the above files, load this file.
  3. At the REPL prompt, type 'tx' to create a transmitting node, or 'rx' to create a receiver node. 
     For a proper demonstration you will need one node of each type.

This code is a good starting point for beginners because it is already working. You can easily modify the code to do whatever transmitting and receiving you want.
