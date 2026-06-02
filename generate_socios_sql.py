import uuid, re

raw_names = '''APPLAUDO STUDIOS, S.A. DE C.V.
ASESORES EN INFORMATICA, S.A. DE C.V.
ASOCIACION SVNET.
Blue Fusion, S.A. de C.V.
CENTRAL AMERICAN SOFTWARE SERVICES, S.A. DE C.V.
CONSORCIO INDUSTRIAL INDEPENDENCIA, S.A. DE .C.V (T-BOX)
CREATIVA CONSULTORES, S.A. DE C.V.
DATAGUARD, S.A. DE C.V.
DATUM, S.A. DE C.V.
ELANIIN
Escuela Superior de Economia y Negocios
GIGA S.A. de C.V.
GRUPO EJJE, S.A. DE C.V.
IT CONSULTING SA DE CV
KORINVER, S.A. DE C.V.
LEGALITIKA
PENSERTRUST, S.A. DE C.V.
PRODUCTIVE BUSINESS SOLUTIONS EL SALVADOR, S.A. DE C.V.
RICOH
SIMPLEXO TECHNOLOGIES, S.A. DE C.V.
Sofis Solutions S. A. de C. V.
TECNOLOGÍAS DE LA INFORMACIÓN Y TALENTOS S.A. DE C.V.(ITProject41)
TIGO EL SALVADOR
UNIVERSIDAD FRANCISCO GAVIDIA
UNIVERSIDAD GERARDO BARRIOS
UNIVERSIDAD PEDAGOGICA DE EL SALVADOR "DR. LUIS ALONSO APARICIO"
UNIVERSIDAD SALVADOREÑA ALBERTO MASFERRER (USAM)
UNIVERSIDAD TECNOLOGICA DE EL SALVADOR
Unplug Studio
2ITJOBS S.A DE C.V
ASESOR DE JUNTA DIRECTIVA
UNIVERSIDAD DR. JOSE MATIAS DELGADO
JUNIOR ACHIEVEMENT EL SALVADOR
Phillips Morris
Devin Tech
7 Sistemas integrados
ACARI STUDIOS
INTELICOLAB
La Constancia
HAKKI
ALFI
N1CO
Pedidos YA
JDK
CONSULTORES DE SISTEMAS, S.A. DE C.V.
RSM
UNIVERSIDAD DE ORIENTE (UNIVO)
INNBOX LTDA de C.V.
BANCO DAVIVIENDA
INNOGEN
LIGHTHOUSE GROUP
EON Consulting
WEB INFORMATICA
STEFANINI
OUTSIDE
ESI
NEXSYS
SOSTENGO
INSPIRETECH  1924
KORINVER, S.A. DE C.V.
CONEXIÓN
DATALYSIS
YOUR NEXT HOP, S.A. DE C.V.
Bridge4 Digital S.A. de C.V.
MYNEFLOW TECHNOLOGIES SAS DE CV
Fabio Emilio Buiza López (Afiliado como persona Natural)
SERVICIOS INTEGRALES DE ASISTENCIA TECNICA S.A DE C.V
CODE CASTLE
CONSULTORES DE SISTEMAS, S.A. DE C.V.
SLR Soluciones
LARS Software Company
ESINTECH
IMPETUS INTERNATIONAL SALVADOR, S.A. DE C.V.
LEXIN CORP
A&E Sistemas, S.A. de C.V.
Administración y Sistemas, S.A. de C.V.
Inversiones Digitales, S.A. de C.V.
AFP Crecer'''

aliases = {
    'APPLAUDO STUDIOS, S.A. DE C.V.': 'Applaudo Studios, S.A. de C.V.',
    'ASESORES EN INFORMATICA, S.A. DE C.V.': 'Asesores en Informática, S.A. de C.V.',
    'ASOCIACION SVNET.': 'Asociación SVNet',
    'Blue Fusion, S.A. de C.V.': 'Blue Fusion, S.A. de C.V.',
    'CENTRAL AMERICAN SOFTWARE SERVICES, S.A. DE C.V.': 'Central American Software Services, S.A. de C.V.',
    'CONSORCIO INDUSTRIAL INDEPENDENCIA, S.A. DE .C.V (T-BOX)': 'Consorcio Industrial Independencia, S.A. de C.V. (T-BOX)',
    'CREATIVA CONSULTORES, S.A. DE C.V.': 'Creativa Consultores, S.A. de C.V.',
    'DATAGUARD, S.A. DE C.V.': 'DATAGUARD, S.A. de C.V.',
    'DATUM, S.A. DE C.V.': 'DATUM, S.A. de C.V.',
    'Escuela Superior de Economia y Negocios': 'Escuela Superior de Economía y Negocios',
    'GIGA S.A. de C.V.': 'GIGA, S.A. de C.V.',
    'GRUPO EJJE, S.A. DE C.V.': 'GRUPO EJJE, S.A. de C.V.',
    'KORINVER, S.A. DE C.V.': 'KORINVER, S.A. de C.V.',
    'PENSERTRUST, S.A. DE C.V.': 'PENSERTRUST, S.A. de C.V.',
    'PRODUCTIVE BUSINESS SOLUTIONS EL SALVADOR, S.A. DE C.V.': 'PRODUCTIVE BUSINESS SOLUTIONS EL SALVADOR, S.A. de C.V.',
    'SIMPLEXO TECHNOLOGIES, S.A. DE C.V.': 'SIMPLEXO TECHNOLOGIES, S.A. de C.V.',
    'Sofis Solutions S. A. de C. V.': 'Sofis Solutions S.A. de C.V.',
    'TECNOLOGÍAS DE LA INFORMACIÓN Y TALENTOS S.A. DE C.V.(ITProject41)': 'TECNOLOGÍAS DE LA INFORMACIÓN Y TALENTOS S.A. de C.V. (ITProject41)',
    'UNIVERSIDAD PEDAGOGICA DE EL SALVADOR "DR. LUIS ALONSO APARICIO"': 'UNIVERSIDAD PEDAGÓGICA DE EL SALVADOR "DR. LUIS ALONSO APARICIO"',
    'UNIVERSIDAD SALVADOREÑA ALBERTO MASFERRER (USAM)': 'UNIVERSIDAD SALVADOREÑA ALBERTO MASFERRER (USAM)',
    'UNIVERSIDAD TECNOLOGICA DE EL SALVADOR': 'UNIVERSIDAD TECNOLÓGICA DE EL SALVADOR',
    '2ITJOBS S.A DE C.V': '2IT Jobs S.A. de C.V.',
    'UNIVERSIDAD DR. JOSE MATIAS DELGADO': 'Universidad Dr. José Matías Delgado',
    'JUNIOR ACHIEVEMENT EL SALVADOR': 'Junior Achievement El Salvador',
    'ACARI STUDIOS': 'Aracari Studios',
    'WEB INFORMATICA': 'WEB INFORMÁTICA',
    'ESI': 'ESI School of Management',
    'INSPIRETECH  1924': 'INSPIRETECH 1924',
    'CONEXIÓN': 'CONEXIÓN',
    'YOUR NEXT HOP, S.A. DE C.V.': 'YOUR NEXT HOP, S.A. de C.V.',
    'Fabio Emilio Buiza López (Afiliado como persona Natural)': 'Fabio Emilio Buiza López',
    'SERVICIOS INTEGRALES DE ASISTENCIA TECNICA S.A DE C.V': 'SERVICIOS INTEGRALES DE ASISTENCIA TÉCNICA S.A. de C.V.',
    'ESINTECH': 'ESINTEC',
    'IMPETUS INTERNATIONAL SALVADOR, S.A. DE C.V.': 'IMPETUS INTERNATIONAL SALVADOR, S.A. de C.V.',
    'LEXIN CORP': 'LEXIN CORP',
}

lines = [line.strip() for line in raw_names.splitlines() if line.strip()]
seen = set(); names = []
for line in lines:
    normalized = aliases.get(line, line).replace('  ', ' ').strip()
    if normalized.endswith('.') and normalized not in {'RSM', 'ALFI', 'HAKKI'}:
        normalized = normalized.rstrip('.')
    if normalized not in seen:
        seen.add(normalized)
        names.append(normalized)

# Additional canonical entries
extra = [
    'A&E Sistemas, S.A. de C.V.',
    'Administración y Sistemas, S.A. de C.V.',
    'Inversiones Digitales, S.A. de C.V.',
    'AFP Crecer'
]
for item in extra:
    if item not in names:
        names.append(item)

trans = str.maketrans('ÁÉÍÓÚÜÑáéíóúüñ', 'AEIOUUNaeiouun')
def slugify(text):
    s = text.translate(trans)
    s = s.lower()
    s = re.sub(r'\s+', '-', s)
    s = re.sub(r'[^a-z0-9\-]', '-', s)
    s = re.sub(r'-+', '-', s)
    return s.strip('-')

def sql_escape(value):
    return value.replace("'", "''")

entries = []
for name in names:
    slug = slugify(name)
    if not slug:
        slug = str(uuid.uuid4())
    entries.append((str(uuid.uuid4()), name, slug))

header = """-- Socios data load generated from user list
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;

TRUNCATE TABLE public.socios RESTART IDENTITY CASCADE;
"""
rows = [header]
for uid, name, slug in entries:
    desc = 'Socio asociado del directorio CASATIC.'
    if any(x in name for x in ['Universidad', 'Escuela']):
        desc = 'Institución académica miembro del directorio CASATIC.'
    elif any(x in name for x in ['Banco', 'AFP', 'Financiera', 'Financiero']):
        desc = 'Entidad financiera asociada al directorio CASATIC.'
    elif any(x in name for x in ['Consultores', 'Consultoría', 'Consultor', 'Consulting']):
        desc = 'Empresa de consultoría tecnológica asociada al directorio CASATIC.'
    elif any(x in name for x in ['Services', 'Solutions', 'Software', 'Tecnologías', 'Tecnologica', 'Technology', 'Tecnologí']):
        desc = 'Empresa tecnológica asociada al directorio CASATIC.'
    elif any(x in name for x in ['Académica', 'Academia', 'Institución', 'Instituto']):
        desc = 'Institución académica miembro del directorio CASATIC.'
    desc = sql_escape(desc)
    rows.append(
        f"INSERT INTO public.socios(\"Id\", \"NombreEmpresa\", \"Slug\", \"Descripcion\", \"Especialidades\", \"Servicios\", \"RsWebsite\", \"RsFacebook\", \"RsLinkedin\", \"RsTwitter\", \"RsInstagram\", \"RsYoutube\", \"Telefono\", \"Direccion\", \"LogoUrl\", \"EmailContacto\", \"MapaUrl\", \"MarcasRepresenta\", \"EstadoFinanciero\", \"Habilitado\") VALUES ('{uid}', '{sql_escape(name)}', '{slug}', '{desc}', '{{}}', '{{}}', '', '', '', '', '', '', '', '', '', '', '', '', 'AlDia', true);"
    )
content = '\n'.join(rows) + '\n'
for filename in [
    'backend/docker script/02-socios-current.sql',
    'backend/docker script/socios-current-backup-2026-05-29_115248.sql',
    'backend/docker script/socios-current-backup-2026-05-29_115248.utf8.sql',
]:
    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
print(f'Wrote {len(entries)} socios entries to SQL files.')
