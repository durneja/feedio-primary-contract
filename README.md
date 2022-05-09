# feedio-primary-contract

Feedio Primary contract is a Smart Contract written in C# that would store the price feed information for the supported tokens. 
It provides two interfaces for retrieving the prices - one for fetching the price for a single token [**getLatestTokenPrice**] and the other for getting the latest prices for all the supported tokens [**getLatestTokenPrices**]
Both these methods validate the caller for the ownership of the Feedio NFT Token that would provide subscription and access rights to the price feed information. 
The method **updateTokenPrice** would be called by the backend to update the supported token prices on-chain.
