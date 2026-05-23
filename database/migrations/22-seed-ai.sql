-- 022-seed-ai.sql
-- Seed data: AI services, models, schemas and prompts

-- ═══════════════════════════════════════════════════════════
-- AI Services
-- ═══════════════════════════════════════════════════════════
INSERT INTO ai_services (name, base_url) VALUES
    ('Google', 'https://generativelanguage.googleapis.com/'),
    ('OpenAI', 'https://api.openai.com/')
ON CONFLICT (name) DO NOTHING;

-- ═══════════════════════════════════════════════════════════
-- AI Models
-- ═══════════════════════════════════════════════════════════
INSERT INTO ai_models (ai_service_id, name, is_default) VALUES
    ((SELECT id FROM ai_services WHERE name = 'Google'), 'gemini-3-flash-preview', FALSE),
    ((SELECT id FROM ai_services WHERE name = 'Google'), 'gemini-3.1-pro-preview', FALSE),
    ((SELECT id FROM ai_services WHERE name = 'Google'), 'gemini-3.5-flash', FALSE),
    ((SELECT id FROM ai_services WHERE name = 'Google'), 'gemini-3.1-flash-lite', TRUE),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-5.4-nano', FALSE),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-5.4-mini', FALSE),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-5.4', FALSE),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-5.4-pro', FALSE),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-5.5', FALSE),
    ((SELECT id FROM ai_services WHERE name = 'OpenAI'), 'gpt-5.5-pro', FALSE)
ON CONFLICT (ai_service_id, name) DO NOTHING;

-- ═══════════════════════════════════════════════════════════
-- AI Schemas
-- ═══════════════════════════════════════════════════════════
INSERT INTO ai_schemas (name, description, json_schema) VALUES

('job_filter', 'Filters job relevance and junior suitability', '{
  "type": "OBJECT",
  "properties": {
    "error": {
      "type": "STRING",
      "description": "Null if successful. Error description if the model cannot determine the result."
    },
    "relevante": {
      "type": "STRING",
      "description": "\"si\" si la oferta es claramente relevante para el perfil, \"no\" si claramente no lo es, \"no_se\" si hay duda."
    },
    "apto_junior": {
      "type": "STRING",
      "description": "\"no\" si la oferta exige explicitamente un perfil senior, o mas de 4 años de experiencia. \"si\" en caso contrario."
    }
  },
  "required": ["relevante", "apto_junior"]
}'),

('keyword_extraction', 'Extracts technologies and company type from job offers', '{
  "type": "OBJECT",
  "properties": {
    "error": {
      "type": "STRING",
      "description": "Null if successful. Error description if extraction fails."
    },
    "skills": {
      "type": "ARRAY",
      "items": { "type": "STRING" },
      "description": "List of technologies, languages, frameworks, tools, and soft skills mentioned in the offer."
    },
    "tipo_empresa": {
      "type": "STRING",
      "description": "Company type: Multinacional, Startup, Pyme, Consultora, or No identificado."
    }
  },
  "required": ["skills", "tipo_empresa"]
}'),

('github_projects', 'Extracts project info from GitHub repositories', '{
  "type": "OBJECT",
  "properties": {
    "error": {
      "type": "STRING",
      "description": "Null if successful. Error description if analysis fails."
    },
    "projects": {
      "type": "ARRAY",
      "items": {
        "type": "OBJECT",
        "properties": {
          "name": { "type": "STRING" },
          "description": { "type": "STRING" },
          "type": { "type": "STRING", "enum": ["personal", "school"] },
          "keywords": {
            "type": "ARRAY",
            "items": { "type": "STRING" }
          }
        },
        "required": ["name", "description", "type", "keywords"]
      }
    }
  },
  "required": ["projects"]
}'),

('keyword_dedup', 'Groups duplicate/similar keywords', '{
  "type": "OBJECT",
  "properties": {
    "error": {
      "type": "STRING",
      "description": "Null if successful. Error description if dedup fails."
    },
    "groups": {
      "type": "ARRAY",
      "items": {
        "type": "ARRAY",
        "items": { "type": "STRING" }
      }
    }
  },
  "required": ["groups"]
}'),

('experience_parse', 'Parses LinkedIn experience text into structured data', '{
  "type": "OBJECT",
  "properties": {
    "error": {
      "type": "STRING",
      "description": "Null if successful. Error description if parsing fails."
    },
    "experiences": {
      "type": "ARRAY",
      "items": {
        "type": "OBJECT",
        "properties": {
          "company": { "type": "STRING" },
          "position": { "type": "STRING" },
          "start_date": { "type": "STRING" },
          "end_date": { "type": "STRING" },
          "description": { "type": "STRING" }
        },
        "required": ["company"]
      }
    }
  },
  "required": ["experiences"]
}'),

('education_parse', 'Parses LinkedIn education text into structured data', '{
  "type": "OBJECT",
  "properties": {
    "error": {
      "type": "STRING",
      "description": "Null if successful. Error description if parsing fails."
    },
    "education": {
      "type": "ARRAY",
      "items": {
        "type": "OBJECT",
        "properties": {
          "institution": { "type": "STRING" },
          "degree": { "type": "STRING" },
          "start_year": { "type": "NUMBER" },
          "end_year": { "type": "NUMBER" }
        },
        "required": ["degree"]
      }
    }
  },
  "required": ["education"]
}'),

('cv_generation', 'Generates structured CV data from user profile and job offer', '{
  "type": "OBJECT",
  "properties": {
    "error": {
      "type": "STRING",
      "description": "Null if successful. Error description if generation fails."
    },
    "contact": {
      "type": "OBJECT",
      "properties": {
        "name": { "type": "STRING" },
        "email": { "type": "STRING" },
        "phone": { "type": "STRING" },
        "linkedin": { "type": "STRING" },
        "github": { "type": "STRING" },
        "location": { "type": "STRING" }
      },
      "required": ["name", "email"]
    },
    "summary": { "type": "STRING" },
    "experiences": {
      "type": "ARRAY",
      "items": {
        "type": "OBJECT",
        "properties": {
          "company": { "type": "STRING" },
          "position": { "type": "STRING" },
          "start_date": { "type": "STRING" },
          "end_date": { "type": "STRING" },
          "highlights": {
            "type": "ARRAY",
            "items": { "type": "STRING" }
          }
        },
        "required": ["company", "position", "highlights"]
      }
    },
    "projects": {
      "type": "ARRAY",
      "items": {
        "type": "OBJECT",
        "properties": {
          "name": { "type": "STRING" },
          "description": { "type": "STRING" },
          "highlights": {
            "type": "ARRAY",
            "items": { "type": "STRING" }
          }
        },
        "required": ["name", "highlights"]
      }
    },
    "education": {
      "type": "ARRAY",
      "items": {
        "type": "OBJECT",
        "properties": {
          "degree": { "type": "STRING" },
          "institution": { "type": "STRING" },
          "year": { "type": "STRING" }
        },
        "required": ["degree", "institution"]
      }
    },
    "skills": {
      "type": "ARRAY",
      "items": {
        "type": "OBJECT",
        "properties": {
          "category": { "type": "STRING" },
          "items": {
            "type": "ARRAY",
            "items": { "type": "STRING" }
          }
        },
        "required": ["category", "items"]
      }
    },
    "languages": {
      "type": "ARRAY",
      "items": { "type": "STRING" }
    }
  },
  "required": ["contact", "summary", "skills"]
}')
ON CONFLICT (name) DO NOTHING;

-- ═══════════════════════════════════════════════════════════
-- AI Prompts
-- ═══════════════════════════════════════════════════════════
INSERT INTO ai_prompts (functionality, name, description, system_prompt, user_prompt_template, schema_id) VALUES

('filter_jobs', 'Filter job relevance', 'Determines if a job offer is relevant and junior-friendly',
'Eres un filtro de ofertas de trabajo especializado en perfiles de Software Engineering.',
'Tu tarea es:
1. Determinar si una oferta de trabajo es RELEVANTE para un perfil de Software Engineer especializado en "{{keyword}}".
2. Determinar si la oferta es APTA PARA UN PERFIL JUNIOR.

CRITERIOS DE RELEVANCIA:
- Puestos directamente relacionados como "{{keyword}} Engineer", "{{keyword}} Developer", etc. son relevantes.
- Puestos de disciplinas cercanas como Firmware, Embedded Systems, Hardware, IoT, RTOS, etc. (segun aplique al keyword) son relevantes.
- Puestos completamente no relacionados como "Sales Manager", "Backend Developer" (si el keyword es Embedded), "Recruiter", etc. NO son relevantes.
- En caso de duda, responde "no_se" en el campo relevante.

CRITERIOS DE PERFIL JUNIOR (apto_junior):
- Responde "no" si la oferta exige EXPLICITAMENTE: perfil "Senior", "Senior Software Engineer", "Lead", "Principal", "Staff Engineer", "Tech Lead", "Engineering Manager", o mas de 4 años de experiencia.
- Responde "si" si la oferta menciona "Junior", "Internship", "Becario", "Graduate", "Entry Level", "Sin experiencia", "0-2 años", "1-3 años", o no especifica nivel de seniority.
- Si la oferta pide "3-4 años" o "Mid-level" o similar, responde "si" (es borde pero aceptable para junior).
- Si no se menciona nada sobre seniority o años de experiencia, responde "si".

Oferta: "{{title}}"
Descripcion: "{{description}}"',
(SELECT id FROM ai_schemas WHERE name = 'job_filter')),

('extract_keywords', 'Extract keywords from job offers', 'Extracts technologies, skills and company type from a job description',
'Eres un analizador de ofertas de trabajo. Extraes tecnologias, skills y tipo de empresa.',
'Analiza esta oferta de trabajo y extrae las tecnologias, lenguajes, herramientas, frameworks, conceptos tecnicos Y habilidades blandas mencionados (comunicacion, liderazgo, trabajo en equipo, etc.). Determina tambien el tipo de empresa.

Oferta: "{{text}}"',
(SELECT id FROM ai_schemas WHERE name = 'keyword_extraction')),

('analyze_github', 'Analyze GitHub repositories', 'Extracts structured project information from GitHub repos',
'Eres un analizador de proyectos de GitHub. Tu tarea es analizar los repositorios de un usuario y extraer informacion estructurada de cada uno.',
'Analiza cada proyecto. Por cada uno debes:
1. Extraer un nombre descriptivo (limpio, sin guiones, max 60 caracteres).
2. Generar una descripcion en castellano (2-4 frases) explicando el proposito, tecnologias usadas y alcance del proyecto.
3. Determinar si es un proyecto PERSONAL o de ESCUELA/BOOTCAMP (type: "personal" o "school"). Si hay README que mencione "42", "42 School", "42 Barcelona", "cursus", "bootcamp" -> es school. Si no se puede determinar -> personal.
4. Extraer una lista EXHAUSTIVA de tecnologias, lenguajes, frameworks, librerias, herramientas y conceptos tecnicos (skills). Incluye TODO lo que veas en el README, package.json, requirements.txt, Makefile, CMakeLists, docker-compose, etc. Se muy minucioso.

Proyectos a analizar:
{{input}}',
(SELECT id FROM ai_schemas WHERE name = 'github_projects')),

('dedup_keywords', 'Deduplicate keywords', 'Groups equivalent/similar keywords into clusters',
'Eres un deduplicador de palabras clave técnicas. Tu tarea es agrupar palabras clave que significan el mismo concepto o area.',
'Agrupa las siguientes palabras clave. Reglas:
- Agrupa términos que se refieran al mismo concepto o área, aunque no sean sinónimos exactos.
- Ejemplos de grupos válidos: ui + ui/ux + ui/ux design + user interface, ai + artificial intelligence + machine learning/ai, aws + amazon web services, docker + docker compose + containerization, react + react.js, node + node.js, python + python3, c# + csharp + .net.
- No agrupes tecnologías claramente diferentes (ej: react y vue NO).
- Cada grupo debe tener las palabras en minúsculas.
- Si una palabra no tiene equivalentes, va en su propio grupo de 1 elemento.
- Devuelve un array de grupos, donde cada grupo es un array de strings equivalentes.

Palabras clave a analizar:
{{keywords}}',
(SELECT id FROM ai_schemas WHERE name = 'keyword_dedup')),

('parse_experience', 'Parse LinkedIn experience', 'Extracts structured work experience from LinkedIn raw text',
'Eres un extractor de datos de LinkedIn. Conviertes texto de experiencias laborales a JSON estructurado.',
'Extrae experiencias laborales a JSON. La linea de fechas SIEMPRE tiene este formato exacto: "mes. año - mes. año · X años/meses".

Ejemplo de linea de fechas: "sept. 2023 - ene. 2024 · 5 meses"
-> start_date: "2023-09-01", end_date: "2024-01-01"

IGNORA la parte "· X años/meses". SOLO extrae las dos fechas de esa linea.
Meses: ene=01 feb=02 mar=03 abr=04 may=05 jun=06 jul=07 ago=08 sept=09 oct=10 nov=11 dic=12

Campos: company, position, start_date, end_date, description

{{raw_text}}',
(SELECT id FROM ai_schemas WHERE name = 'experience_parse')),

('parse_education', 'Parse LinkedIn education', 'Extracts structured education from LinkedIn raw text',
'Eres un extractor de datos de LinkedIn. Conviertes texto de educacion a JSON estructurado.',
'Extrae educacion a JSON. La linea de fechas tiene formato: "mes. año – mes. año".

Ej: "ene. 2024 – may. 2025" -> start_year:2024, end_year:2025
Ej: "sept. 2009 – jun. 2011" -> start_year:2009, end_year:2011
Solo extrae el año (4 digitos).

Campos: institution, degree, start_year, end_year.
Ignora "Aptitudes:", "Actividades y grupos:".

{{raw_text}}',
(SELECT id FROM ai_schemas WHERE name = 'education_parse')),

('cv_generation', 'Generate CV', 'Generates a structured CV from user profile and job offer',
'Eres un generador de CVs profesionales optimizados para ATS (Applicant Tracking Systems). Genera datos estructurados para un CV personalizado.',
'Genera un CV en el mismo idioma que la oferta de trabajo. Si la oferta esta en español, CV en español. Si en ingles, CV en ingles.

OFERTA DE TRABAJO:
Titulo: {{job_title}}
Empresa: {{company}}
Descripcion: {{job_description}}
Keywords de la oferta: {{job_keywords}}

PERFIL DEL USUARIO:
Nombre: {{user_name}}
Email: {{user_email}}
Telefono: {{user_phone}}
Ubicacion: {{user_location}}
LinkedIn: {{user_linkedin}}
GitHub: {{user_github}}
Presentacion: {{user_presentation}}
Idiomas: {{user_languages}}

EXPERIENCIA:
{{user_experiences}}

EDUCACION:
{{user_education}}

PROYECTOS:
{{user_projects}}

KEYWORDS DEL USUARIO (aprendidas): {{user_keywords}}

INSTRUCCIONES:
1. Contact: nombre completo, email, telefono, linkedin, github, ubicacion.
2. Summary: 3-4 lineas en el idioma de la oferta destacando la experiencia mas relevante.
3. Experiences: MINIMO 1, MAXIMO 3. Las mas relevantes primero, con highlights que usen keywords de la oferta.
4. Projects: MINIMO 1, MAXIMO 3. Los mas relevantes primero.
5. Education: maximo 3, las mas recientes primero.
6. Skills: Agrupadas por categorias (Backend, Frontend, Databases, DevOps, AI, Tools, Soft Skills...). MINIMO 8 skills por categoria. Si la oferta menciona soft skills, incluye categoria Soft Skills con al menos 8 habilidades blandas.
7. Languages: solo nombres (sin nivel).

PASO FINAL: Revisa el CV contra la oferta. Si falta alguna keyword importante que el usuario conoce, añadela. Si alguna experiencia puede describirse mejor para este puesto, mejoralo.',
(SELECT id FROM ai_schemas WHERE name = 'cv_generation'))
ON CONFLICT (functionality) DO NOTHING;
