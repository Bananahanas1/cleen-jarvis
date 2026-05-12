# GRAPH_REPORT PART 07

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---

Nodes (22): _init_project(), Tests for ``specify integration`` subcommand (list, install, uninstall, switch)., Install into a project with .specify/ but no integration., Helper: init a spec-kit project with the given integration., Installing into a bare project should create shared scripts and templates., Full lifecycle: install → modify → uninstall → modified file kept., Shared scripts and templates are not removed by integration uninstall., Switching preserves shared scripts, templates, and memory. (+14 more)

### Community 72 - "Community 72"
Cohesion: 0.09
Nodes (17): DatabaseQueryTool, Tests for the db_query tool., Tests for DatabaseQueryTool., Test querying a real SQLite file., Sensitive file patterns (e.g. .env) should be blocked., Sensitive file patterns (.pem) should be blocked., WITH ... SELECT (CTE) should be allowed in read-only mode., WITH ... INSERT should be blocked in read-only mode. (+9 more)

### Community 73 - "Community 73"
Cohesion: 0.06
Nodes (5): TemplateRepository, addCamelCaseVariations(), addRelatedNodeTypes(), generateTemplateNodeVariations(), resolveTemplateNodeTypes()

### Community 74 - "Community 74"
Cohesion: 0.07
Nodes (22): cli(), main(), Command-line interface for OpenJarvis (Click-based)., Entry point registered as ``jarvis`` console script., Global logging configuration for the OpenJarvis CLI., Formatter that redacts credentials from log messages., Configure the ``openjarvis`` logger.      Parameters     ----------     verbose:, SanitizingFormatter (+14 more)

### Community 75 - "Community 75"
Cohesion: 0.06
Nodes (5): NeuroLinkedBridge, NeuroLinkedSummary, VoicePipeline, ChatDatabase, IDisposable

### Community 76 - "Community 76"
Cohesion: 0.09
Nodes (19): AllowedRoot, _is_blocked(), _is_under_allowed_root(), load_mount_allowlist(), Mount validation and security for container sandboxes.  Port of NanoClaw's ``mou, Check whether any component of *mount_path* matches a block pattern., Check whether *mount_path* is under any allowed root., Validate a single mount path against the allowlist.      Returns ``True`` if the (+11 more)

### Community 77 - "Community 77"
Cohesion: 0.1
Nodes (19): Tests for VLLMMetricsScraper — Prometheus text format parsing., TestParseGauge, TestParseHistogramBuckets, TestPercentileFromBuckets, TestVLLMMetricsDataclass, TestVLLMMetricsScraper, _parse_gauge(), _parse_histogram_buckets() (+11 more)

### Community 78 - "Community 78"
Cohesion: 0.09
Nodes (18): main(), normalizeHookResult(), readStdinRaw(), runHooks(), runPostBash(), runPreBash(), emitHookResult(), getPluginRoot() (+10 more)

### Community 79 - "Community 79"
Cohesion: 0.08
Nodes (31): EmbeddingStore, EmbeddingStore --- disk-persistent ColBERT token-level embeddings.  Stores per-c, Load a chunk's embedding from disk.          Returns ``None`` if the chunk has n, Check if embeddings exist for a chunk., Return the number of stored embeddings., Delete the embedding for a chunk.          Returns ``True`` if the embedding exi, Close the underlying SQLite connection., Stores ColBERT token-level embeddings on disk.      Each ``chunk_id`` maps to a (+23 more)

### Community 80 - "Community 80"
Cohesion: 0.16
Nodes (24): make_sample(), ring_buffer_basic_push_and_len(), ring_buffer_clear(), ring_buffer_exact_fill(), ring_buffer_ordered_no_wrap(), ring_buffer_single_capacity(), ring_buffer_wrap_around(), ring_buffer_zero_capacity_panics() (+16 more)

### Community 81 - "Community 81"
Cohesion: 0.06
Nodes (19): GraphEvents(), FocusOnNode(), GraphControl(), WorkerLayoutControl(), useAppState(), usePagination(), usePaginationFor(), useSettings() (+11 more)

### Community 82 - "Community 82"
Cohesion: 0.13
Nodes (21): Steady-state detection for energy measurement at thermal equilibrium., Clear all recorded state., Configuration for steady-state detection., Result of steady-state detection., Detect steady state using coefficient of variation over a sliding window.      T, Record a sample.  Returns ``True`` when steady state is reached., result(), SteadyStateConfig (+13 more)

### Community 83 - "Community 83"
Cohesion: 0.06
Nodes (19): Tests for the ``jarvis config`` CLI commands., Test that config show toml displays the raw TOML content., Test that config show json displays parsed TOML as JSON., Test that config show hardware displays hardware information., Test cases for the jarvis config CLI group., Test that config show (no subcommand) defaults to loaded., Test that config show handles missing config file gracefully., Test that the config group help displays correctly. (+11 more)

### Community 84 - "Community 84"
Cohesion: 0.11
Nodes (17): compute_efficiency(), EfficiencyMetrics, estimate_model_bytes_per_token(), estimate_model_flops_per_token(), MFU/MBU efficiency calculator for GPU inference telemetry.  Computes Model FLOPs, Results of an MFU/MBU efficiency calculation., Estimate FLOPs for one forward-pass token of a dense transformer.      For dense, Estimate bytes of memory loaded per decode step.      Args:         param_count_ (+9 more)

### Community 85 - "Community 85"
Cohesion: 0.09
Nodes (26): main(), parseArgs(), requireValue(), showHelp(), Renderer, bucketByDay(), formatPercent(), getTrendArrow() (+18 more)

### Community 86 - "Community 86"
Cohesion: 0.1
Nodes (19): CopyStats, main(), parse_args(), QdrantLegacyDataPreparationTool, Tool for preparing legacy data in Qdrant for migration testing, Initialize the tool.          Args:             workspace: Workspace to use f, Get or create QdrantClient instance, Check Qdrant connection (+11 more)

### Community 87 - "Community 87"
Cohesion: 0.07
Nodes (30): anthropic_complete(), anthropic_complete_if_cache(), anthropic_embed(), claude_3_haiku_complete(), claude_3_opus_complete(), claude_3_sonnet_complete(), InvalidResponseError, Deprecated alias for :func:`lightrag.llm.voyageai.voyageai_embed`.      This s (+22 more)

### Community 88 - "Community 88"
Cohesion: 0.07
Nodes (12): Grandparent, Child, Parent, run(), Child, run(), Child, Grandparent (+4 more)

### Community 89 - "Community 89"
Cohesion: 0.11
Nodes (4): MigrationRunner, Check if a specific label is present., getStarterQueries(), StarterCard()

### Community 90 - "Community 90"
Cohesion: 0.07
Nodes (8): DummyAsyncContext, DummyGraphStorage, DummyMergeGraphStorage, DummyVectorStorage, test_aedit_entity_allows_updates_without_description(), test_handle_single_relationship_extraction_ignores_empty_description(), test_merge_entities_preserves_file_path_in_vector_updates(), test_merge_nodes_then_upsert_handles_missing_legacy_description()

### Community 91 - "Community 91"
Cohesion: 0.16
Nodes (8): error(), header(), info(), log(), ReleasePreparation, success(), warning(), NodeMigrationService

### Community 92 - "Community 92"
Cohesion: 0.09
Nodes (6): Address, City, get_user(), getUser(), Repo, User

### Community 93 - "Community 93"
Cohesion: 0.13
Nodes (23): get_provider(), is_cloud_model(), list_local_models(), _load_keys(), _ollama_host(), Direct cloud API router — bypasses the engine system entirely.  Reads API keys f, Return (system_text, chat_messages) in Anthropic format., Convert to Google Gemini content format. (+15 more)

### Community 94 - "Community 94"
Cohesion: 0.11
Nodes (13): compute_mfu(), estimate_flops(), estimate_flops_no_kv_cache(), _get_params_b(), FLOPs estimation and Model FLOPs Utilization (MFU) computation., Look up model parameter count (billions)., Estimate FLOPs for an inference pass (assumes KV caching).      Uses the 2 * P *, Estimate FLOPs without KV caching (full recompute per token).      Without KV ca (+5 more)

### Community 95 - "Community 95"
Cohesion: 0.11
Nodes (23): AgentTemplate, _builtin_templates_dir(), discover_templates(), load_template(), Agent template loader — load pre-configured agent manifests from TOML files., A pre-configured agent manifest loaded from a TOML template., Load an agent template from a TOML file.      Expected format::          [templa, Return the path to the built-in templates shipped with the package. (+15 more)

### Community 96 - "Community 96"
Cohesion: 0.16
Nodes (19): generate_keypair(), KeyPair, load_public_key(), Ed25519 signing — supply chain integrity for agent and skill manifests., Load a raw 32-byte Ed25519 public key from a file., Save keypair to files (base64-encoded)., Generate a new Ed25519 key pair.      Requires the ``cryptography`` package, Sign *data* with an Ed25519 *private_key*.      Returns the raw 64-byte signatur (+11 more)

### Community 97 - "Community 97"
Cohesion: 0.13
Nodes (6): _parse_pages(), PDFExtractTool, spec(), Tests for the pdf_extract tool., TestParsePages, TestPDFExtractTool

### Community 98 - "Community 98"
Cohesion: 0.11
Nodes (18): _make_app(), _make_generate_only_engine(), _make_streaming_engine(), Tests for the WebSocket streaming endpoint., Sending non-JSON text should return an error., An empty string for 'message' should return an error., When the engine has no stream(), generate() result is sent as one chunk., The model field from the request should be forwarded to the engine. (+10 more)

### Community 99 - "Community 99"
Cohesion: 0.11
Nodes (1): NodeSpecificValidators

### Community 100 - "Community 100"
Cohesion: 0.15
Nodes (25): animate(), buildEdges(), buildNodes(), createSkillNodes(), distance(), drawBackdrop(), drawEdges(), drawNodeGlows() (+17 more)

### Community 101 - "Community 101"
Cohesion: 0.12
Nodes (13): build_safe_env(), kill_process_tree(), Subprocess sandbox — secure process execution with environment isolation., Result of a sandboxed subprocess execution., Build a sanitized environment dict.      Only copies safe vars from current env,, Kill a process and all its children (best effort)., Execute a command in a sandboxed subprocess.      Features:     - Clean environm, run_sandboxed() (+5 more)

### Community 102 - "Community 102"
Cohesion: 0.13
Nodes (15): compute_itl_stats(), itl_stats_basic(), itl_stats_empty(), itl_stats_percentile_interpolation(), itl_stats_single_timestamp(), itl_stats_two_timestamps(), itl_stats_varying_latencies(), ItlStats (+7 more)

### Community 103 - "Community 103"
Cohesion: 0.07
Nodes (26): AnotherNode, ArrayNode, ComplexNode, createLoaderWithMocks(), CustomNode, DashNode, DefaultNode, DotNode (+18 more)

### Community 104 - "Community 104"
Cohesion: 0.1
Nodes (9): PyAgentContext, PyAgentResult, PyConfig, PyEventBus, PyMessage, PyModelSpec, PyRoutingContext, PyToolCall (+1 more)

### Community 105 - "Community 105"
Cohesion: 0.13
Nodes (14): Tests for WASM sandbox (Phase 16.4)., test_validate_invalid_bytes(), TestCreateSandboxRunner, TestWasmRunner, available(), create_sandbox_runner(), WASM sandbox — lightweight isolation via Wasmtime., Result from a WASM execution. (+6 more)

### Community 106 - "Community 106"
Cohesion: 0.17
