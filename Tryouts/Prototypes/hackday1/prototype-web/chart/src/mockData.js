export default class Mocks {
    constructor(){
        this.mock_buy = new Map();
        this.mock_sell = new Map();
        
       this.mock_buy.set('IBM', [49.9, 71.5, 106.4, 129.2, 144.0, 63.45, 89.13, 15.26, 203.2, 58.7, 115.4, 32.8]);
       this.mock_sell.set('IBM', [83.6, 78.8, 98.5, 93.4, 106.0, 82.3, 45.6, 305.6, 263.5, 33.5, 112.6, 87.3]);
        
       this.mock_buy.set('AAPL', [ 18, 49, 62, 110, 134, 162, 166, 210, 215, 277, 290, 297]);
       this.mock_sell.set('AAPL', [62, 93, 114, 140, 150, 161, 191, 206, 224, 255, 286, 295]);

       this.mock_buy.set('TSLA', [33, 93, 124, 177, 187, 210, 225, 234, 236, 247, 250, 282]);
       this.mock_sell.set('TSLA', [33, 62, 64, 94, 124, 166, 186, 191, 225, 247, 267, 294]);

       this.mock_buy.set('SMSN', [14, 25, 73, 84, 138, 155, 181, 195, 200, 209, 231, 254]);
       this.mock_sell.set('SMSN', [30, 37, 67, 86, 182, 199, 219, 225, 238, 245, 250, 262]);
        
       this.mock_buy.set('GOOG', [25, 42, 43, 175, 189, 190, 201, 218, 223, 231, 263, 284]);
       this.mock_sell.set('GOOG', [28, 29, 57, 109, 129, 184, 196, 224, 230, 259, 277, 278]); 
    }

    getBuyDataBySymbol = function(symbol){
        return this.mock_buy.get(symbol);
    }

    getSellDataBySymbol = function(symbol){
        return this.mock_sell.get(symbol);
    }
}