-- Revert false positives from clean_keywords (gpt-4o-mini run)
-- These valid tech keywords were incorrectly flagged and blocked+deleted.
-- Step 1: Unblock them so future fetches can re-insert them.
-- Step 2: Re-insert into keywords (ON CONFLICT DO NOTHING = safe if already there).

DELETE FROM blocked_keywords
WHERE name IN (
  -- Batch 1
  'ctf', 'ctp', 'cube', 'cursor', 'cyberchef', 'dam',
  'datasheets', 'datos', 'daw', 'datacenter', 'dataloggers',
  -- Batch 2
  'codecs', 'collaboration', 'commerce', 'commissioning', 'compilation',
  'compliance', 'connectivity', 'container', 'converters', 'copyloading',
  'copywriting', 'csa', 'crewai', 'cricket', 'costes', 'cowork',
  'cpn', 'cpu', 'cra',
  -- Batch 3
  'av', 'backlog', 'backup', 'belts', 'brazo', 'bridging',
  'bronze', 'build',
  -- Batch 4
  'c+c++', 'c/c++', 'cagas', 'calidad', 'cand-fd', 'canoe',
  'clause', 'climate',
  -- Batch 5
  'anthropic', 'anti-bot', 'apache', 'api', 'array',
  'automation', 'automotivacion', 'autonomy',
  -- Batch 6
  'a-i-a-s', 'ablation', 'abx', 'accelerate', 'accuracy', 'acero',
  'acting', 'ad/ldap/smb/ssh', 'adaptability', 'add-ins', 'adversary',
  'agents', 'aggregations', 'ahena', 'ai', 'albedo',
  'alucinaciones', 'aluminio'
);

-- Re-insert valid keywords (id is auto-generated, created_at defaults to NOW())
INSERT INTO keywords (name) VALUES
  ('ctf'), ('ctp'), ('cube'), ('cursor'), ('cyberchef'), ('dam'),
  ('datasheets'), ('datos'), ('daw'), ('datacenter'), ('dataloggers'),
  ('codecs'), ('collaboration'), ('commerce'), ('commissioning'), ('compilation'),
  ('compliance'), ('connectivity'), ('container'), ('converters'), ('copyloading'),
  ('copywriting'), ('csa'), ('crewai'), ('cricket'), ('costes'), ('cowork'),
  ('cpn'), ('cpu'), ('cra'),
  ('av'), ('backlog'), ('backup'), ('belts'), ('brazo'), ('bridging'),
  ('bronze'), ('build'),
  ('c+c++'), ('c/c++'), ('cagas'), ('calidad'), ('cand-fd'), ('canoe'),
  ('clause'), ('climate'),
  ('anthropic'), ('anti-bot'), ('apache'), ('api'), ('array'),
  ('automation'), ('automotivacion'), ('autonomy'),
  ('a-i-a-s'), ('ablation'), ('abx'), ('accelerate'), ('accuracy'), ('acero'),
  ('acting'), ('ad/ldap/smb/ssh'), ('adaptability'), ('add-ins'), ('adversary'),
  ('agents'), ('aggregations'), ('ahena'), ('ai'), ('albedo'),
  ('alucinaciones'), ('aluminio')
ON CONFLICT (name) DO NOTHING;
