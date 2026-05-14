export interface ProviderStatus {
  name: string;
  role: string;
  configured: boolean;
  envKey: string;
  model?: string;
}

export interface SetupStatus {
  providers: ProviderStatus[];
  ready: string[];
  missing: string[];
  cloudReady: boolean;
  databasePath: string;
  browser: {
    engine: string;
    autorun: boolean;
    denyTerms: string[];
    mode: string;
  };
}

export interface AmyRow {
  id: number;
  title?: string;
  body?: string;
  details?: string;
  state?: string;
  kind?: string;
  status?: string;
  summary?: string;
  created_at: string;
}
