export type Quote = {
  symbol: string
  bid: number
  ask: number
  last: number
  open: number
  close: number
  bidSize: number
  askSize: number
  timestamp: string
}

export type QuoteEdit = Pick<Quote, 'symbol' | 'bid' | 'ask' | 'bidSize' | 'askSize'>
