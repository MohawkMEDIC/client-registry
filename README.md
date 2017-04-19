# MEDIC Client Registry

<div class="wikidoc">
<p>The<a title="Mohawk MEDIC Centre" href="http://www.mohawkmedic.org" target="_blank"> Mohawk College</a> MARC-HI/MEDIC Client Registry Reference Implementation represents the prototype Client Registry (Enterprise Master Patient Index - EMPI) developed under
 our Natural Sciences and Engineering Research Council of Canada (NSERC) grant to build a test version of the pan-Canadian Electronic Health Record System blueprint as prescribed by Canada Health Infoway.</p>
<p>The reference implementation software supports many standards based interfaces including:</p>
<ul>
<li>Support for pan-Canadian Messaging (HL7v3) R02.04.01 </li><li>IHE PIX Version 3&nbsp; </li><li>IHE PDQ Version 3&nbsp; </li><li>IHE PIX v2.x (HL7v2.3.1 ADT feed, verified IHE CAT NA2015) </li><li>IHE PDQ v2.x (HL7v2.5,verified IHE CAT NA2015) </li><li>IHE PDQm (verified IHE CAT NA2015) </li></ul>
<p>In addition to these standards based interface, the client registry:</p>
<ul>
<li>Can act as a Patient Identity Feed (PIXv3) to other actors, </li><li>Supports RFC-3881 (ATNA for IHE interfaces) auditing, </li><li>Provides support for advanced matching/merging algorithms, </li><li>Soundex Matching </li><li>Pattern Matching </li><li>Name Variant Matching </li><li>Provides a custom management interface for merging duplicate patient information,
</li><li>Provides an easy-to-use configuration/deployment tool, </li><li>Provides a highly scalable infrastructure via support for PostgreSQL synchronous streaming replication
</li><li>Supports query continuation, and persistence, </li><li>Supports message logging and long-term execute-once detection, </li><li>Provides a highly extensible platform for custom interfaces and modules. </li></ul>
<p>This reference implementation project is intended to assist developers in the development of Client Registry software, customer interfaces (as a test interface), in demonstration XDS infrastructures, or in staging environments.</p>
<p>For more information about this project please contact Duane Bender, Director of Applied Research in Digital Health, Mohawk College [duane.bender at mohawkcollege dot ca]</p>
</div><div class="ClearBoth"></div>
