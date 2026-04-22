import { useEffect, useMemo, useRef, useState } from 'react'
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { AgGridReact } from 'ag-grid-react'
import type {
  CellValueChangedEvent,
  ColDef,
  GridApi,
  GridReadyEvent,
  ValueFormatterParams,
  ValueParserParams,
} from 'ag-grid-community'
import { AllCommunityModule, ModuleRegistry } from 'ag-grid-community'
import type { Quote, QuoteEdit } from './types/quote'
import 'ag-grid-community/styles/ag-grid.css'
import 'ag-grid-community/styles/ag-theme-quartz.css'
import './App.css'

ModuleRegistry.registerModules([AllCommunityModule])

const HUB_URL = import.meta.env.VITE_HUB_URL ?? 'http://localhost:5051/hubs/quotes'

const moneyFormatter = new Intl.NumberFormat('en-US', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

const sizeFormatter = new Intl.NumberFormat('en-US')

const parseNumber = (params: ValueParserParams<Quote>) => {
  const parsed = Number(params.newValue)
  return Number.isFinite(parsed) ? parsed : params.oldValue
}

function App() {
  const [status, setStatus] = useState('Connecting...')
  const [snapshot, setSnapshot] = useState<Quote[]>([])
  const connectionRef = useRef<HubConnection | null>(null)
  const gridApiRef = useRef<GridApi<Quote> | null>(null)
  const knownSymbolsRef = useRef<Set<string>>(new Set())

  const columnDefs = useMemo<ColDef<Quote>[]>(() => [
    { field: 'symbol', headerName: 'Symbol', width: 110 },
    {
      field: 'bid',
      editable: true,
      valueParser: parseNumber,
      valueFormatter: (params: ValueFormatterParams<Quote>) => moneyFormatter.format(params.value ?? 0),
      type: 'numericColumn',
    },
    {
      field: 'ask',
      editable: true,
      valueParser: parseNumber,
      valueFormatter: (params: ValueFormatterParams<Quote>) => moneyFormatter.format(params.value ?? 0),
      type: 'numericColumn',
    },
    {
      field: 'last',
      valueFormatter: (params: ValueFormatterParams<Quote>) => moneyFormatter.format(params.value ?? 0),
      type: 'numericColumn',
    },
    {
      field: 'open',
      valueFormatter: (params: ValueFormatterParams<Quote>) => moneyFormatter.format(params.value ?? 0),
      type: 'numericColumn',
    },
    {
      field: 'close',
      valueFormatter: (params: ValueFormatterParams<Quote>) => moneyFormatter.format(params.value ?? 0),
      type: 'numericColumn',
    },
    {
      field: 'bidSize',
      headerName: 'Bid Size',
      editable: true,
      valueParser: parseNumber,
      valueFormatter: (params: ValueFormatterParams<Quote>) => sizeFormatter.format(params.value ?? 0),
      type: 'numericColumn',
    },
    {
      field: 'askSize',
      headerName: 'Ask Size',
      editable: true,
      valueParser: parseNumber,
      valueFormatter: (params: ValueFormatterParams<Quote>) => sizeFormatter.format(params.value ?? 0),
      type: 'numericColumn',
    },
    {
      field: 'timestamp',
      valueFormatter: (params: ValueFormatterParams<Quote>) => {
        if (!params.value) {
          return ''
        }

        return new Date(params.value).toLocaleTimeString()
      },
      width: 130,
    },
  ], [])

  useEffect(() => {
    let isMounted = true
    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on('ReceiveSnapshot', (quotes: Quote[]) => {
      knownSymbolsRef.current = new Set(quotes.map((q) => q.symbol))
      setSnapshot(quotes)
      gridApiRef.current?.setGridOption('rowData', quotes)
    })

    connection.on('ReceiveQuote', (quote: Quote) => {
      const knownSymbols = knownSymbolsRef.current
      const api = gridApiRef.current

      if (!api) {
        return
      }

      if (knownSymbols.has(quote.symbol)) {
        api.applyTransactionAsync({ update: [quote] })
        return
      }

      knownSymbols.add(quote.symbol)
      api.applyTransactionAsync({ add: [quote] })
    })

    connection.onreconnecting(() => setStatus('Reconnecting...'))
    connection.onreconnected(() => setStatus('Connected'))
    connection.onclose(() => setStatus('Disconnected'))

    const startConnection = () => {
      connection
        .start()
        .then(() => {
          if (!isMounted) {
            return
          }

          setStatus('Connected')
          connectionRef.current = connection
        })
        .catch(() => {
          if (!isMounted) {
            return
          }

          setStatus('Connection failed, retrying...')
          window.setTimeout(startConnection, 2000)
        })
    }

    startConnection()

    return () => {
      isMounted = false
      connectionRef.current = null
      void connection.stop()
    }
  }, [])

  const onGridReady = (event: GridReadyEvent<Quote>) => {
    gridApiRef.current = event.api
    knownSymbolsRef.current = new Set(snapshot.map((q) => q.symbol))
  }

  const onCellValueChanged = (event: CellValueChangedEvent<Quote>) => {
    if (!event.data || event.newValue === event.oldValue) {
      return
    }

    const payload: QuoteEdit = {
      symbol: event.data.symbol,
      bid: event.data.bid,
      ask: event.data.ask,
      bidSize: Math.trunc(event.data.bidSize),
      askSize: Math.trunc(event.data.askSize),
    }

    void connectionRef.current?.invoke('UpdateQuote', payload)
  }

  return (
    <main>
      <header>
        <h1>TickerScout Live Quotes</h1>
        <p>
          SignalR status: <strong>{status}</strong>
        </p>
      </header>
      <section className="ag-theme-quartz grid-shell">
        <AgGridReact<Quote>
          theme="legacy"
          rowData={snapshot}
          columnDefs={columnDefs}
          defaultColDef={{ resizable: true, sortable: true, flex: 1 }}
          getRowId={(params) => params.data.symbol}
          onGridReady={onGridReady}
          onCellValueChanged={onCellValueChanged}
          animateRows
        />
      </section>
    </main>
  )
}

export default App
