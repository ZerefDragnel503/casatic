--
-- PostgreSQL database dump
--

\restrict T9UEBRjIXASPhBpqNy5DMVSJJu76d23dfhOxMmmuTdAYv1eVHURALogwnhYRKiE

-- Dumped from database version 16.13
-- Dumped by pg_dump version 16.13

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: pgcrypto; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS pgcrypto WITH SCHEMA public;


--
-- Name: EXTENSION pgcrypto; Type: COMMENT; Schema: -; Owner: 
--

COMMENT ON EXTENSION pgcrypto IS 'cryptographic functions';


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: eventos; Type: TABLE; Schema: public; Owner: casatic
--

CREATE TABLE public.eventos (
    "Id" uuid DEFAULT gen_random_uuid() NOT NULL,
    "SocioId" uuid NOT NULL,
    "UsuarioId" uuid,
    "Titulo" character varying(300) NOT NULL,
    "Slug" character varying(300) NOT NULL,
    "Descripcion" text NOT NULL,
    "Tipo" character varying(50) NOT NULL,
    "Modalidad" character varying(20) NOT NULL,
    "FechaInicio" timestamp with time zone NOT NULL,
    "FechaFin" timestamp with time zone,
    "Lugar" text DEFAULT ''::text NOT NULL,
    "ImageUrl" text DEFAULT ''::text NOT NULL,
    "Estado" character varying(20) DEFAULT 'Pendiente'::character varying NOT NULL,
    "Habilitado" boolean DEFAULT true NOT NULL,
    "Destacado" boolean DEFAULT false NOT NULL,
    "PublicadoAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL,
    "UpdatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.eventos OWNER TO casatic;

--
-- Name: facturas; Type: TABLE; Schema: public; Owner: casatic
--

CREATE TABLE public.facturas (
    "Id" uuid DEFAULT gen_random_uuid() NOT NULL,
    "SocioId" uuid NOT NULL,
    "Numero" character varying(40) NOT NULL,
    "PlanNombre" character varying(120) NOT NULL,
    "PlanPeriodo" character varying(40) NOT NULL,
    "Descripcion" text NOT NULL,
    "Subtotal" numeric(12,2) NOT NULL,
    "Iva" numeric(12,2) NOT NULL,
    "Total" numeric(12,2) NOT NULL,
    "Estado" character varying(20) DEFAULT 'Pendiente'::character varying NOT NULL,
    "FechaEmision" timestamp with time zone DEFAULT now() NOT NULL,
    "FechaVencimiento" timestamp with time zone NOT NULL,
    "FechaPago" timestamp with time zone,
    "Notas" text DEFAULT ''::text NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL,
    "UpdatedAt" timestamp with time zone DEFAULT now() NOT NULL,
    "TipoDocumento" character varying(60) DEFAULT 'Factura interna'::character varying NOT NULL,
    "CodigoGeneracion" character varying(40) DEFAULT ''::character varying NOT NULL,
    "NumeroControl" character varying(60) DEFAULT ''::character varying NOT NULL,
    "SelloRecepcion" character varying(120) DEFAULT ''::character varying NOT NULL,
    "Ambiente" character varying(30) DEFAULT 'Produccion'::character varying NOT NULL,
    "CondicionOperacion" character varying(30) DEFAULT 'Credito'::character varying NOT NULL,
    "FormaPago" character varying(60) DEFAULT 'Transferencia'::character varying NOT NULL,
    "ReferenciaPago" character varying(120) DEFAULT ''::character varying NOT NULL
);


ALTER TABLE public.facturas OWNER TO casatic;

--
-- Name: formularios_contacto; Type: TABLE; Schema: public; Owner: casatic
--

CREATE TABLE public.formularios_contacto (
    "Id" uuid DEFAULT gen_random_uuid() NOT NULL,
    "SocioId" uuid NOT NULL,
    "Nombre" character varying(200) NOT NULL,
    "Correo" character varying(256) NOT NULL,
    "Mensaje" text NOT NULL,
    "Fecha" timestamp with time zone DEFAULT now() NOT NULL,
    "Leido" boolean DEFAULT false NOT NULL
);


ALTER TABLE public.formularios_contacto OWNER TO casatic;

--
-- Name: logs_actividad; Type: TABLE; Schema: public; Owner: casatic
--

CREATE TABLE public.logs_actividad (
    "Id" uuid DEFAULT gen_random_uuid() NOT NULL,
    "TipoEvento" character varying(30) NOT NULL,
    "Fecha" timestamp with time zone DEFAULT now() NOT NULL,
    "UsuarioId" uuid,
    "SocioId" uuid,
    "Ip" character varying(45) DEFAULT NULL::character varying,
    "UserAgent" text,
    "Query" text
);


ALTER TABLE public.logs_actividad OWNER TO casatic;

--
-- Name: socios; Type: TABLE; Schema: public; Owner: casatic
--

CREATE TABLE public.socios (
    "Id" uuid DEFAULT gen_random_uuid() NOT NULL,
    "NombreEmpresa" character varying(300) NOT NULL,
    "Slug" character varying(300) NOT NULL,
    "Descripcion" text DEFAULT ''::text NOT NULL,
    "Especialidades" text[] DEFAULT '{}'::text[] NOT NULL,
    "Servicios" text[] DEFAULT '{}'::text[] NOT NULL,
    "RsWebsite" character varying(500) DEFAULT ''::character varying NOT NULL,
    "RsFacebook" character varying(500) DEFAULT ''::character varying NOT NULL,
    "RsLinkedin" character varying(500) DEFAULT ''::character varying NOT NULL,
    "RsTwitter" character varying(500) DEFAULT ''::character varying NOT NULL,
    "RsInstagram" character varying(500) DEFAULT ''::character varying NOT NULL,
    "RsYoutube" character varying(500) DEFAULT ''::character varying NOT NULL,
    "Telefono" text DEFAULT ''::text NOT NULL,
    "Direccion" text DEFAULT ''::text NOT NULL,
    "LogoUrl" text DEFAULT ''::text NOT NULL,
    "EmailContacto" text DEFAULT ''::text NOT NULL,
    "MapaUrl" text DEFAULT ''::text NOT NULL,
    "MarcasRepresenta" text DEFAULT ''::text NOT NULL,
    "EstadoFinanciero" character varying(20) DEFAULT 'AlDia'::character varying NOT NULL,
    "Habilitado" boolean DEFAULT true NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL,
    "UpdatedAt" timestamp with time zone DEFAULT now() NOT NULL,
    "SearchVector" tsvector GENERATED ALWAYS AS (to_tsvector('spanish'::regconfig, (((COALESCE("NombreEmpresa", ''::character varying))::text || ' '::text) || COALESCE("Descripcion", ''::text)))) STORED
);


ALTER TABLE public.socios OWNER TO casatic;

--
-- Name: usuarios; Type: TABLE; Schema: public; Owner: casatic
--

CREATE TABLE public.usuarios (
    "Id" uuid DEFAULT gen_random_uuid() NOT NULL,
    "Email" character varying(256) NOT NULL,
    "PasswordHash" text NOT NULL,
    "Rol" character varying(20) DEFAULT 'Usuario'::character varying NOT NULL,
    "PrimerLogin" boolean DEFAULT true NOT NULL,
    "Activo" boolean DEFAULT true NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL,
    "TokenRecuperacion" character varying(500) DEFAULT NULL::character varying,
    "FechaExpiracionToken" timestamp with time zone,
    "SocioId" uuid
);


ALTER TABLE public.usuarios OWNER TO casatic;

--
-- Data for Name: eventos; Type: TABLE DATA; Schema: public; Owner: casatic
--

COPY public.eventos ("Id", "SocioId", "UsuarioId", "Titulo", "Slug", "Descripcion", "Tipo", "Modalidad", "FechaInicio", "FechaFin", "Lugar", "ImageUrl", "Estado", "Habilitado", "Destacado", "PublicadoAt", "CreatedAt", "UpdatedAt") FROM stdin;
9515088c-6d4e-42b1-b0fc-f198711cc5c8	3e50e6fe-ea40-4bb7-a960-f416297f4207	\N	Webinar de Ciberseguridad Empresarial	webinar-ciberseguridad-empresarial	Buenas practicas de seguridad informatica para empresas.	Webinar	Virtual	2026-06-07 17:42:50.059072+00	2026-06-07 19:42:50.059072+00	Online		Aprobado	t	f	2026-06-02 17:42:50.059072+00	2026-06-02 17:42:50.059071+00	2026-06-02 17:42:50.059071+00
beefddcb-521a-4fb5-8927-ad8d7f3dd0cc	3e50e6fe-ea40-4bb7-a960-f416297f4207	\N	Conferencia de Innovacion CASATIC	conferencia-innovacion-casatic	Evento enfocado en transformacion digital, innovacion y tecnologia empresarial.	Conferencia	Presencial	2026-06-12 17:42:50.059021+00	2026-06-12 21:42:50.059028+00	San Salvador, El Salvador		Aprobado	t	t	2026-06-02 17:42:50.059065+00	2026-06-02 17:42:50.058959+00	2026-06-02 17:42:50.058959+00
\.


--
-- Data for Name: facturas; Type: TABLE DATA; Schema: public; Owner: casatic
--

COPY public.facturas ("Id", "SocioId", "Numero", "PlanNombre", "PlanPeriodo", "Descripcion", "Subtotal", "Iva", "Total", "Estado", "FechaEmision", "FechaVencimiento", "FechaPago", "Notas", "CreatedAt", "UpdatedAt", "TipoDocumento", "CodigoGeneracion", "NumeroControl", "SelloRecepcion", "Ambiente", "CondicionOperacion", "FormaPago", "ReferenciaPago") FROM stdin;
c567d49a-a9f3-4051-9a5e-ee2fa975b232	3e50e6fe-ea40-4bb7-a960-f416297f4207	CAS-2026-0001	Socios Miembros	anual	Membresia anual CASATIC - Socios Miembros	400.00	52.00	452.00	Pendiente	2026-06-02 17:42:50.20698+00	2026-07-02 17:42:50.206987+00	\N	Factura generada automaticamente desde el plan de membresia publicado en el home.	2026-06-02 17:42:50.204783+00	2026-06-02 17:42:50.204783+00	Factura interna	08B222E5-B548-48DC-857F-0DAD6442A407	DTE-01-CASATIC-2026-0001		Produccion	Credito	Transferencia	
\.


--
-- Data for Name: formularios_contacto; Type: TABLE DATA; Schema: public; Owner: casatic
--

COPY public.formularios_contacto ("Id", "SocioId", "Nombre", "Correo", "Mensaje", "Fecha", "Leido") FROM stdin;
\.


--
-- Data for Name: logs_actividad; Type: TABLE DATA; Schema: public; Owner: casatic
--

COPY public.logs_actividad ("Id", "TipoEvento", "Fecha", "UsuarioId", "SocioId", "Ip", "UserAgent", "Query") FROM stdin;
\.


--
-- Data for Name: socios; Type: TABLE DATA; Schema: public; Owner: casatic
--

COPY public.socios ("Id", "NombreEmpresa", "Slug", "Descripcion", "Especialidades", "Servicios", "RsWebsite", "RsFacebook", "RsLinkedin", "RsTwitter", "RsInstagram", "RsYoutube", "Telefono", "Direccion", "LogoUrl", "EmailContacto", "MapaUrl", "MarcasRepresenta", "EstadoFinanciero", "Habilitado", "CreatedAt", "UpdatedAt") FROM stdin;
3e50e6fe-ea40-4bb7-a960-f416297f4207	Empresa de Prueba	empresa-prueba	Empresa de prueba para validar el sistema.	{Software,Consultoria}	{Desarrollo,Asesoria}													AlDia	t	2026-06-02 17:42:49.463978+00	2026-06-02 17:42:49.463978+00
\.


--
-- Data for Name: usuarios; Type: TABLE DATA; Schema: public; Owner: casatic
--

COPY public.usuarios ("Id", "Email", "PasswordHash", "Rol", "PrimerLogin", "Activo", "CreatedAt", "TokenRecuperacion", "FechaExpiracionToken", "SocioId") FROM stdin;
81800757-37d2-4939-899f-d41df1ecab90	prueba@prueba.com	$2a$11$79sEbpxRrOiRNjMijC0dOOmadlhl0brY5p1RrOylh8IYS8clXlkN.	Socio	t	t	2026-06-02 17:42:49.815685+00	\N	\N	3e50e6fe-ea40-4bb7-a960-f416297f4207
b270b1f6-c7df-411b-bac6-89ebae4641f2	admin@casatic.org	$2a$11$HzvxH1h7OFnxr91V.Bg.VecbBVZu2lMLRLD6VBLDu3X99plI5c3TG	Admin	t	t	2026-06-02 17:42:49.647402+00	\N	\N	\N
\.


--
-- Name: eventos eventos_Slug_key; Type: CONSTRAINT; Schema: public; Owner: casatic
--

ALTER TABLE ONLY public.eventos
    ADD CONSTRAINT "eventos_Slug_key" UNIQUE ("Slug");


--
-- Name: facturas facturas_Numero_key; Type: CONSTRAINT; Schema: public; Owner: casatic
--

ALTER TABLE ONLY public.facturas
    ADD CONSTRAINT "facturas_Numero_key" UNIQUE ("Numero");


--
-- Name: eventos pk_eventos; Type: CONSTRAINT; Schema: public; Owner: casatic
--

ALTER TABLE ONLY public.eventos
    ADD CONSTRAINT pk_eventos PRIMARY KEY ("Id");


--
-- Name: facturas pk_facturas; Type: CONSTRAINT; Schema: public; Owner: casatic
--

ALTER TABLE ONLY public.facturas
    ADD CONSTRAINT pk_facturas PRIMARY KEY ("Id");


--
-- Name: formularios_contacto pk_formularios_contacto; Type: CONSTRAINT; Schema: public; Owner: casatic
--

ALTER TABLE ONLY public.formularios_contacto
    ADD CONSTRAINT pk_formularios_contacto PRIMARY KEY ("Id");


--
-- Name: logs_actividad pk_logs_actividad; Type: CONSTRAINT; Schema: public; Owner: casatic
--

ALTER TABLE ONLY public.logs_actividad
    ADD CONSTRAINT pk_logs_actividad PRIMARY KEY ("Id");


--
-- Name: socios pk_socios; Type: CONSTRAINT; Schema: public; Owner: casatic
--

ALTER TABLE ONLY public.socios
    ADD CONSTRAINT pk_socios PRIMARY KEY ("Id");


--
-- Name: usuarios pk_usuarios; Type: CONSTRAINT; Schema: public; Owner: casatic
--

ALTER TABLE ONLY public.usuarios
    ADD CONSTRAINT pk_usuarios PRIMARY KEY ("Id");


--
-- Name: socios socios_Slug_key; Type: CONSTRAINT; Schema: public; Owner: casatic
--

ALTER TABLE ONLY public.socios
    ADD CONSTRAINT "socios_Slug_key" UNIQUE ("Slug");


--
-- Name: usuarios usuarios_Email_key; Type: CONSTRAINT; Schema: public; Owner: casatic
--

ALTER TABLE ONLY public.usuarios
    ADD CONSTRAINT "usuarios_Email_key" UNIQUE ("Email");


--
-- Name: ix_eventos_destacado; Type: INDEX; Schema: public; Owner: casatic
--

CREATE INDEX ix_eventos_destacado ON public.eventos USING btree ("Destacado");


--
-- Name: ix_eventos_estado; Type: INDEX; Schema: public; Owner: casatic
--

CREATE INDEX ix_eventos_estado ON public.eventos USING btree ("Estado");


--
-- Name: ix_eventos_fecha_inicio; Type: INDEX; Schema: public; Owner: casatic
--

CREATE INDEX ix_eventos_fecha_inicio ON public.eventos USING btree ("FechaInicio");


--
-- Name: ix_eventos_slug; Type: INDEX; Schema: public; Owner: casatic
--

CREATE INDEX ix_eventos_slug ON public.eventos USING btree ("Slug");


--
-- Name: ix_eventos_socio_id; Type: INDEX; Schema: public; Owner: casatic
--

CREATE INDEX ix_eventos_socio_id ON public.eventos USING btree ("SocioId");


--
-- Name: ix_eventos_usuario_id; Type: INDEX; Schema: public; Owner: casatic
--

CREATE INDEX ix_eventos_usuario_id ON public.eventos USING btree ("UsuarioId");


--
-- Name: ix_facturas_estado; Type: INDEX; Schema: public; Owner: casatic
--

CREATE INDEX ix_facturas_estado ON public.facturas USING btree ("Estado");


--
-- Name: ix_facturas_fecha_vencimiento; Type: INDEX; Schema: public; Owner: casatic
--

CREATE INDEX ix_facturas_fecha_vencimiento ON public.facturas USING btree ("FechaVencimiento");


--
-- Name: ix_facturas_numero; Type: INDEX; Schema: public; Owner: casatic
--

CREATE UNIQUE INDEX ix_facturas_numero ON public.facturas USING btree ("Numero");


--
-- Name: ix_facturas_socio_id; Type: INDEX; Schema: public; Owner: casatic
--

CREATE UNIQUE INDEX ix_facturas_socio_id ON public.facturas USING btree ("SocioId");


--
-- Name: ix_formularios_contacto_socio_id; Type: INDEX; Schema: public; Owner: casatic
--

CREATE INDEX ix_formularios_contacto_socio_id ON public.formularios_contacto USING btree ("SocioId");


--
-- Name: ix_logs_actividad_fecha; Type: INDEX; Schema: public; Owner: casatic
--

CREATE INDEX ix_logs_actividad_fecha ON public.logs_actividad USING btree ("Fecha" DESC);


--
-- Name: ix_logs_actividad_socio_id; Type: INDEX; Schema: public; Owner: casatic
--

CREATE INDEX ix_logs_actividad_socio_id ON public.logs_actividad USING btree ("SocioId");


--
-- Name: ix_logs_actividad_tipo_evento; Type: INDEX; Schema: public; Owner: casatic
--

CREATE INDEX ix_logs_actividad_tipo_evento ON public.logs_actividad USING btree ("TipoEvento");


--
-- Name: ix_logs_actividad_usuario_id; Type: INDEX; Schema: public; Owner: casatic
--

CREATE INDEX ix_logs_actividad_usuario_id ON public.logs_actividad USING btree ("UsuarioId");


--
-- Name: ix_socios_search_vector; Type: INDEX; Schema: public; Owner: casatic
--

CREATE INDEX ix_socios_search_vector ON public.socios USING gin ("SearchVector");


--
-- Name: ix_socios_slug; Type: INDEX; Schema: public; Owner: casatic
--

CREATE INDEX ix_socios_slug ON public.socios USING btree ("Slug");


--
-- Name: ix_usuarios_email; Type: INDEX; Schema: public; Owner: casatic
--

CREATE INDEX ix_usuarios_email ON public.usuarios USING btree ("Email");


--
-- Name: ix_usuarios_socio_id; Type: INDEX; Schema: public; Owner: casatic
--

CREATE INDEX ix_usuarios_socio_id ON public.usuarios USING btree ("SocioId");


--
-- Name: eventos fk_eventos_socios; Type: FK CONSTRAINT; Schema: public; Owner: casatic
--

ALTER TABLE ONLY public.eventos
    ADD CONSTRAINT fk_eventos_socios FOREIGN KEY ("SocioId") REFERENCES public.socios("Id") ON DELETE CASCADE;


--
-- Name: eventos fk_eventos_usuarios; Type: FK CONSTRAINT; Schema: public; Owner: casatic
--

ALTER TABLE ONLY public.eventos
    ADD CONSTRAINT fk_eventos_usuarios FOREIGN KEY ("UsuarioId") REFERENCES public.usuarios("Id") ON DELETE SET NULL;


--
-- Name: facturas fk_facturas_socios; Type: FK CONSTRAINT; Schema: public; Owner: casatic
--

ALTER TABLE ONLY public.facturas
    ADD CONSTRAINT fk_facturas_socios FOREIGN KEY ("SocioId") REFERENCES public.socios("Id") ON DELETE CASCADE;


--
-- Name: formularios_contacto fk_formularios_socios; Type: FK CONSTRAINT; Schema: public; Owner: casatic
--

ALTER TABLE ONLY public.formularios_contacto
    ADD CONSTRAINT fk_formularios_socios FOREIGN KEY ("SocioId") REFERENCES public.socios("Id") ON DELETE CASCADE;


--
-- Name: logs_actividad logs_actividad_SocioId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: casatic
--

ALTER TABLE ONLY public.logs_actividad
    ADD CONSTRAINT "logs_actividad_SocioId_fkey" FOREIGN KEY ("SocioId") REFERENCES public.socios("Id") ON DELETE SET NULL;


--
-- Name: logs_actividad logs_actividad_UsuarioId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: casatic
--

ALTER TABLE ONLY public.logs_actividad
    ADD CONSTRAINT "logs_actividad_UsuarioId_fkey" FOREIGN KEY ("UsuarioId") REFERENCES public.usuarios("Id") ON DELETE SET NULL;


--
-- Name: usuarios usuarios_SocioId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: casatic
--

ALTER TABLE ONLY public.usuarios
    ADD CONSTRAINT "usuarios_SocioId_fkey" FOREIGN KEY ("SocioId") REFERENCES public.socios("Id") ON DELETE SET NULL;


--
-- PostgreSQL database dump complete
--

\unrestrict T9UEBRjIXASPhBpqNy5DMVSJJu76d23dfhOxMmmuTdAYv1eVHURALogwnhYRKiE

